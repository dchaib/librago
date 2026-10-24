using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Librago.Persistence;

public sealed partial class LibragoDatabase
{
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
                last_complete_success_account_ids TEXT NULL,
                result TEXT NOT NULL
            );

            PRAGMA user_version = 1;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }


    private static async Task ApplyVersionTwoAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE reservations (
                account_id TEXT NOT NULL,
                external_id TEXT NOT NULL,
                network_key TEXT NOT NULL,
                network_name TEXT NOT NULL,
                snapshot TEXT NOT NULL,
                first_observed_on TEXT NOT NULL,
                first_available_on TEXT NULL,
                PRIMARY KEY (account_id, external_id)
            );
            CREATE TABLE reservation_network_sync (
                network_key TEXT PRIMARY KEY,
                network_name TEXT NOT NULL,
                last_attempt_at TEXT NOT NULL,
                last_complete_success_at TEXT NULL,
                last_complete_success_account_ids TEXT NULL,
                result TEXT NOT NULL
            );
            PRAGMA user_version = 2;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ApplyVersionThreeAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ALTER TABLE account_sync RENAME TO loan_account_sync;
            ALTER TABLE network_sync RENAME TO loan_network_sync;
            PRAGMA user_version = 3;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ApplyVersionFourAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DROP TABLE loan_account_sync;
            PRAGMA user_version = 4;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ApplyVersionFiveAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using (var alter = connection.CreateCommand())
        {
            alter.Transaction = transaction;
            alter.CommandText = "ALTER TABLE loans ADD COLUMN first_observed_on TEXT NULL;";
            await alter.ExecuteNonQueryAsync(cancellationToken);
        }
        var observations = new List<(string AccountId, string ExternalId, DateOnly Date)>();
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT account_id, external_id, refreshed_at FROM loans WHERE first_observed_on IS NULL;";
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                observations.Add((reader.GetString(0), reader.GetString(1),
                    ObservationDate(DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture))));
        }
        foreach (var observation in observations)
        {
            await using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE loans SET first_observed_on = $date WHERE account_id = $account AND external_id = $id;";
            update.Parameters.AddWithValue("$date", FormatDate(observation.Date));
            update.Parameters.AddWithValue("$account", observation.AccountId);
            update.Parameters.AddWithValue("$id", observation.ExternalId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE loans_new (
                account_id TEXT NOT NULL,
                external_id TEXT NOT NULL,
                network_key TEXT NOT NULL,
                network_name TEXT NOT NULL,
                borrower TEXT NOT NULL,
                title TEXT NOT NULL,
                author TEXT NULL,
                material_type TEXT NULL,
                library_id TEXT NULL,
                library TEXT NULL,
                borrowed_on TEXT NULL,
                due_on TEXT NOT NULL,
                first_observed_on TEXT NOT NULL,
                PRIMARY KEY (account_id, external_id)
            );
            INSERT INTO loans_new SELECT account_id, external_id, network_key, network_name,
                borrower, title, author, material_type, NULL, branch, borrowed_on, due_on, first_observed_on FROM loans;
            DROP TABLE loans;
            ALTER TABLE loans_new RENAME TO loans;
            CREATE INDEX ix_loans_due_on ON loans (due_on);
            CREATE INDEX ix_loans_network_key ON loans (network_key);
            CREATE INDEX ix_loans_borrower ON loans (borrower);
            DROP TABLE reservations;
            DELETE FROM reservation_network_sync;
            CREATE TABLE reservations (
                account_id TEXT NOT NULL,
                external_id TEXT NOT NULL,
                network_key TEXT NOT NULL,
                network_name TEXT NOT NULL,
                borrower TEXT NOT NULL,
                title TEXT NOT NULL,
                status TEXT NOT NULL,
                status_label TEXT NULL,
                author TEXT NULL,
                material_type TEXT NULL,
                pickup_library_id TEXT NULL,
                pickup_library TEXT NULL,
                reserved_on TEXT NULL,
                available_on TEXT NULL,
                pickup_deadline TEXT NULL,
                queue_position INTEGER NULL,
                suspension_starts_on TEXT NULL,
                suspension_ends_on TEXT NULL,
                first_observed_on TEXT NOT NULL,
                first_available_on TEXT NULL,
                PRIMARY KEY (account_id, external_id)
            );
            PRAGMA user_version = 5;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

}
