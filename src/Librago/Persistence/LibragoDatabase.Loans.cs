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

        var previous = new Dictionary<string, DateOnly>(StringComparer.Ordinal);
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT external_id, first_observed_on FROM loans WHERE account_id = $account AND network_key = $network;";
            read.Parameters.AddWithValue("$account", account.AccountId);
            read.Parameters.AddWithValue("$network", network.Key);
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                previous.Add(reader.GetString(0), ReadNullableDate(reader, 1)!.Value);
        }

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
                previous.TryGetValue(loan.ExternalId, out var first) ? first : ObservationDate(attemptedAt),
                cancellationToken);
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
                library_id,
                library,
                borrowed_on,
                due_on,
                first_observed_on
            FROM loans
            ORDER BY due_on, title;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            loans.Add(new Loan(
                reader.GetString(0),
                reader.GetString(2),
                reader.GetString(3),
                new LoanSnapshot(
                    reader.GetString(1),
                    reader.GetString(4),
                    reader.GetString(5),
                    ReadNullableString(reader, 6),
                    ReadNullableString(reader, 7),
                    ReadNullableString(reader, 8),
                    ReadNullableString(reader, 9),
                    ReadNullableDate(reader, 10),
                    DateOnly.ParseExact(reader.GetString(11), "yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ReadNullableDate(reader, 12)!.Value));
        }

        return loans;
    }


    private static async Task InsertLoanAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        LibraryAccountOptions account,
        LibraryNetworkDescriptor network,
        LoanSnapshot loan,
        DateOnly firstObservedOn,
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
                library_id,
                library,
                borrowed_on,
                due_on,
                first_observed_on)
            VALUES (
                $accountId,
                $externalId,
                $networkKey,
                $networkName,
                $borrower,
                $title,
                $author,
                $materialType,
                $libraryId,
                $library,
                $borrowedOn,
                $dueOn,
                $firstObservedOn);
            """;
        command.Parameters.AddWithValue("$accountId", account.AccountId);
        command.Parameters.AddWithValue("$externalId", loan.ExternalId);
        command.Parameters.AddWithValue("$networkKey", network.Key);
        command.Parameters.AddWithValue("$networkName", network.DisplayName);
        command.Parameters.AddWithValue("$borrower", loan.Borrower);
        command.Parameters.AddWithValue("$title", loan.Title);
        command.Parameters.AddWithValue("$author", (object?)loan.Author ?? DBNull.Value);
        command.Parameters.AddWithValue("$materialType", (object?)loan.MaterialType ?? DBNull.Value);
        command.Parameters.AddWithValue("$libraryId", (object?)loan.LibraryId ?? DBNull.Value);
        command.Parameters.AddWithValue("$library", (object?)loan.Library ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$borrowedOn",
            loan.BorrowedOn is null ? DBNull.Value : FormatDate(loan.BorrowedOn.Value));
        command.Parameters.AddWithValue("$dueOn", FormatDate(loan.DueOn));
        command.Parameters.AddWithValue("$firstObservedOn", FormatDate(firstObservedOn));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }


}
