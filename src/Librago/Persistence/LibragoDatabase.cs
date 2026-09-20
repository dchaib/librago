using System.Globalization;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Loans;
using Librago.Synchronization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Librago.Persistence;

public sealed class LibragoDatabase
{
    private const int CurrentSchemaVersion = 1;
    private readonly string _connectionString;

    public LibragoDatabase(IOptions<LibragoOptions> options, IWebHostEnvironment environment)
    {
        var configuredPath = options.Value.DatabasePath;
        var databasePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }

        var version = await ReadSchemaVersionAsync(connection, cancellationToken);
        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"The database schema version {version} is newer than this application supports.");
        }

        if (version < 1)
        {
            await ApplyVersionOneAsync(connection, cancellationToken);
        }
    }

    public async Task ReplaceAccountLoansAsync(
        LibraryAccountOptions account,
        LibraryNetworkDescriptor network,
        IReadOnlyCollection<LoanSnapshot> loans,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM loans WHERE account_id = $accountId;";
            delete.Parameters.AddWithValue("$accountId", account.AccountId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var loan in loans)
        {
            await InsertLoanAsync(
                connection,
                transaction,
                account,
                network,
                loan,
                attemptedAt,
                cancellationToken);
        }

        await using (var state = connection.CreateCommand())
        {
            state.Transaction = transaction;
            state.CommandText =
                """
                INSERT INTO account_sync (account_id, network_key, last_attempt_at, last_success_at, result)
                VALUES ($accountId, $networkKey, $attemptedAt, $attemptedAt, 'Success')
                ON CONFLICT(account_id) DO UPDATE SET
                    network_key = excluded.network_key,
                    last_attempt_at = excluded.last_attempt_at,
                    last_success_at = excluded.last_success_at,
                    result = excluded.result;
                """;
            state.Parameters.AddWithValue("$accountId", account.AccountId);
            state.Parameters.AddWithValue("$networkKey", network.Key);
            state.Parameters.AddWithValue("$attemptedAt", FormatTimestamp(attemptedAt));
            await state.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveUnconfiguredAccountsAsync(
        IEnumerable<string> configuredAccountIds,
        CancellationToken cancellationToken)
    {
        var accountIds = configuredAccountIds.ToArray();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        foreach (var table in new[] { "loans", "account_sync" })
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;

            if (accountIds.Length == 0)
            {
                command.CommandText = $"DELETE FROM {table};";
            }
            else
            {
                var parameterNames = accountIds
                    .Select((_, index) => $"$accountId{index}")
                    .ToArray();
                command.CommandText =
                    $"DELETE FROM {table} WHERE account_id NOT IN ({string.Join(", ", parameterNames)});";

                for (var index = 0; index < accountIds.Length; index++)
                {
                    command.Parameters.AddWithValue(parameterNames[index], accountIds[index]);
                }
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MarkAccountFailedAsync(
        LibraryAccountOptions account,
        LibraryNetworkDescriptor network,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO account_sync (account_id, network_key, last_attempt_at, last_success_at, result)
            VALUES ($accountId, $networkKey, $attemptedAt, NULL, 'Failed')
            ON CONFLICT(account_id) DO UPDATE SET
                network_key = excluded.network_key,
                last_attempt_at = excluded.last_attempt_at,
                result = excluded.result;
            """;
        command.Parameters.AddWithValue("$accountId", account.AccountId);
        command.Parameters.AddWithValue("$networkKey", network.Key);
        command.Parameters.AddWithValue("$attemptedAt", FormatTimestamp(attemptedAt));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetNetworkStateAsync(
        string networkKey,
        string networkName,
        DateTimeOffset attemptedAt,
        SynchronizationResult result,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO network_sync (
                network_key,
                network_name,
                last_attempt_at,
                last_complete_success_at,
                result)
            VALUES (
                $networkKey,
                $networkName,
                $attemptedAt,
                CASE WHEN $result = 'Success' THEN $attemptedAt ELSE NULL END,
                $result)
            ON CONFLICT(network_key) DO UPDATE SET
                network_name = excluded.network_name,
                last_attempt_at = excluded.last_attempt_at,
                last_complete_success_at = CASE
                    WHEN excluded.result = 'Success' THEN excluded.last_attempt_at
                    ELSE network_sync.last_complete_success_at
                END,
                result = excluded.result;
            """;
        command.Parameters.AddWithValue("$networkKey", networkKey);
        command.Parameters.AddWithValue("$networkName", networkName);
        command.Parameters.AddWithValue("$attemptedAt", FormatTimestamp(attemptedAt));
        command.Parameters.AddWithValue("$result", result.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetLoansAsync(CancellationToken cancellationToken)
    {
        var loans = new List<Loan>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                account_id,
                external_id,
                network_key,
                network_name,
                borrower,
                title,
                author,
                material_type,
                branch,
                borrowed_on,
                due_on,
                refreshed_at
            FROM loans
            ORDER BY due_on, title;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(new Loan(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                ReadNullableString(reader, 6),
                ReadNullableString(reader, 7),
                ReadNullableString(reader, 8),
                ReadNullableDate(reader, 9),
                DateOnly.ParseExact(reader.GetString(10), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(11), CultureInfo.InvariantCulture)));
        }

        return loans;
    }

    public async Task<IReadOnlyList<NetworkSynchronizationState>> GetNetworkStatesAsync(
        CancellationToken cancellationToken)
    {
        var states = new List<NetworkSynchronizationState>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT network_key, network_name, last_attempt_at, last_complete_success_at, result
            FROM network_sync
            ORDER BY network_name;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            states.Add(new NetworkSynchronizationState(
                reader.GetString(0),
                reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                reader.IsDBNull(3)
                    ? null
                    : DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                Enum.Parse<SynchronizationResult>(reader.GetString(4))));
        }

        return states;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            return Convert.ToInt32(
                await command.ExecuteScalarAsync(cancellationToken),
                CultureInfo.InvariantCulture) == 1;
        }
        catch (SqliteException)
        {
            return false;
        }
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<int> ReadSchemaVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task ApplyVersionOneAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            CREATE TABLE loans (
                account_id TEXT NOT NULL,
                external_id TEXT NOT NULL,
                network_key TEXT NOT NULL,
                network_name TEXT NOT NULL,
                borrower TEXT NOT NULL,
                title TEXT NOT NULL,
                author TEXT NULL,
                material_type TEXT NULL,
                branch TEXT NULL,
                borrowed_on TEXT NULL,
                due_on TEXT NOT NULL,
                refreshed_at TEXT NOT NULL,
                PRIMARY KEY (account_id, external_id)
            );

            CREATE INDEX ix_loans_due_on ON loans (due_on);
            CREATE INDEX ix_loans_network_key ON loans (network_key);
            CREATE INDEX ix_loans_borrower ON loans (borrower);

            CREATE TABLE account_sync (
                account_id TEXT PRIMARY KEY,
                network_key TEXT NOT NULL,
                last_attempt_at TEXT NOT NULL,
                last_success_at TEXT NULL,
                result TEXT NOT NULL
            );

            CREATE TABLE network_sync (
                network_key TEXT PRIMARY KEY,
                network_name TEXT NOT NULL,
                last_attempt_at TEXT NOT NULL,
                last_complete_success_at TEXT NULL,
                result TEXT NOT NULL
            );

            PRAGMA user_version = 1;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task InsertLoanAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        LibraryAccountOptions account,
        LibraryNetworkDescriptor network,
        LoanSnapshot loan,
        DateTimeOffset refreshedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO loans (
                account_id,
                external_id,
                network_key,
                network_name,
                borrower,
                title,
                author,
                material_type,
                branch,
                borrowed_on,
                due_on,
                refreshed_at)
            VALUES (
                $accountId,
                $externalId,
                $networkKey,
                $networkName,
                $borrower,
                $title,
                $author,
                $materialType,
                $branch,
                $borrowedOn,
                $dueOn,
                $refreshedAt);
            """;
        command.Parameters.AddWithValue("$accountId", account.AccountId);
        command.Parameters.AddWithValue("$externalId", loan.ExternalId);
        command.Parameters.AddWithValue("$networkKey", network.Key);
        command.Parameters.AddWithValue("$networkName", network.DisplayName);
        command.Parameters.AddWithValue("$borrower", loan.Borrower);
        command.Parameters.AddWithValue("$title", loan.Title);
        command.Parameters.AddWithValue("$author", (object?)loan.Author ?? DBNull.Value);
        command.Parameters.AddWithValue("$materialType", (object?)loan.MaterialType ?? DBNull.Value);
        command.Parameters.AddWithValue("$branch", (object?)loan.Branch ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$borrowedOn",
            loan.BorrowedOn is null ? DBNull.Value : FormatDate(loan.BorrowedOn.Value));
        command.Parameters.AddWithValue("$dueOn", FormatDate(loan.DueOn));
        command.Parameters.AddWithValue("$refreshedAt", FormatTimestamp(refreshedAt));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateOnly? ReadNullableDate(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : DateOnly.ParseExact(reader.GetString(ordinal), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
