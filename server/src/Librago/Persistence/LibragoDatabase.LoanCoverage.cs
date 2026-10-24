using Librago.Loans;
using Librago.Synchronization;
using Microsoft.Data.Sqlite;

namespace Librago.Persistence;

public sealed record LoanAccountCoverage(string NetworkKey, string AccountId, DateTimeOffset LastSuccessAt);
public sealed record LoanReadSnapshot(
    IReadOnlyList<Loan> Loans,
    IReadOnlyList<NetworkSynchronizationState> NetworkStates,
    IReadOnlyList<LoanAccountCoverage> Coverage);

public sealed partial class LibragoDatabase
{
    private static async Task ApplyVersionSixAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE loan_account_coverage (
                network_key TEXT NOT NULL,
                account_id TEXT NOT NULL COLLATE NOCASE,
                last_success_at TEXT NOT NULL,
                PRIMARY KEY (network_key, account_id)
            );
            INSERT INTO loan_account_coverage
            SELECT s.network_key, j.value, s.last_complete_success_at
            FROM loan_network_sync s, json_each(s.last_complete_success_account_ids) j
            WHERE s.last_complete_success_at IS NOT NULL AND j.type = 'text';
            PRAGMA user_version = 6;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<LoanReadSnapshot> ReadLoanSnapshotAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: true);
        var loans = await ReadLoansAsync(connection, transaction, cancellationToken);
        var states = await ReadLoanNetworkStatesAsync(connection, transaction, cancellationToken);
        var coverage = new List<LoanAccountCoverage>();
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT network_key, account_id, last_success_at FROM loan_account_coverage;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                coverage.Add(new(reader.GetString(0), reader.GetString(1),
                    DateTimeOffset.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture)));
        }
        await transaction.CommitAsync(cancellationToken);
        return new(loans, states, coverage);
    }
}
