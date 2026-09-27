using System.Globalization;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Loans;
using Microsoft.Data.Sqlite;

namespace Librago.Persistence;

public sealed partial class LibragoDatabase
{
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


}