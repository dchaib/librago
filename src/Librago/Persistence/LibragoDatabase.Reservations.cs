using System.Text.Json;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Reservations;
using Microsoft.Data.Sqlite;

namespace Librago.Persistence;

public sealed partial class LibragoDatabase
{
    public async Task ReplaceAccountReservationsAsync(
        LibraryAccountOptions account, LibraryNetworkDescriptor network,
        IReadOnlyList<ReservationSnapshot> reservations, DateOnly observedOn,
        CancellationToken cancellationToken)
    {
        if (reservations.Any(r => string.IsNullOrWhiteSpace(r.ExternalId) ||
                string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.Borrower)) ||
            reservations.Select(r => r.ExternalId).Distinct(StringComparer.Ordinal).Count() != reservations.Count)
        {
            throw new LibraryConnectorException(LibraryConnectorFailureKind.InvalidData,
                "The reservation snapshot contains missing fields or duplicate identities.");
        }
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var previous = new Dictionary<string, (DateOnly Observed, DateOnly? Available)>(StringComparer.Ordinal);
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT external_id, first_observed_on, first_available_on FROM reservations WHERE account_id = $account AND network_key = $network;";
            read.Parameters.AddWithValue("$account", account.AccountId);
            read.Parameters.AddWithValue("$network", network.Key);
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                previous.Add(reader.GetString(0), (ReadNullableDate(reader, 1)!.Value, ReadNullableDate(reader, 2)));
        }
        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM reservations WHERE account_id = $account;";
            delete.Parameters.AddWithValue("$account", account.AccountId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var item in reservations)
        {
            var first = previous.TryGetValue(item.ExternalId, out var dates) ? dates.Observed : observedOn;
            var available = dates.Available ?? (item.Status == ReservationStatus.Available ? observedOn : (DateOnly?)null);
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO reservations VALUES ($account, $id, $network, $name, $snapshot, $first, $available);";
            insert.Parameters.AddWithValue("$account", account.AccountId);
            insert.Parameters.AddWithValue("$id", item.ExternalId);
            insert.Parameters.AddWithValue("$network", network.Key);
            insert.Parameters.AddWithValue("$name", network.DisplayName);
            insert.Parameters.AddWithValue("$snapshot", JsonSerializer.Serialize(item));
            insert.Parameters.AddWithValue("$first", FormatDate(first));
            insert.Parameters.AddWithValue("$available", available is { } date ? FormatDate(date) : DBNull.Value);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetReservationsAsync(CancellationToken cancellationToken)
    {
        var reservations = new List<Reservation>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT account_id, network_key, network_name, snapshot, first_observed_on, first_available_on FROM reservations;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            reservations.Add(new Reservation(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                JsonSerializer.Deserialize<ReservationSnapshot>(reader.GetString(3))!,
                ReadNullableDate(reader, 4)!.Value, ReadNullableDate(reader, 5)));
        return reservations;
    }
}
