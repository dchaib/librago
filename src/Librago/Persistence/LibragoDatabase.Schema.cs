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

}
