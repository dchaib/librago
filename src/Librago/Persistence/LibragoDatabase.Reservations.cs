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
            await InsertReservationAsync(connection, transaction,
                new Reservation(account.AccountId, network.Key, network.DisplayName, item, first, available),
                cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetReservationsAsync(CancellationToken cancellationToken)
    {
        var reservations = new List<Reservation>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT account_id, network_key, network_name, external_id, borrower, title, status,
                status_label, author, material_type, pickup_library_id, pickup_library, reserved_on,
                available_on, pickup_deadline, queue_position, suspension_starts_on, suspension_ends_on,
                first_observed_on, first_available_on FROM reservations;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            reservations.Add(new Reservation(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                new ReservationSnapshot(reader.GetString(3), reader.GetString(4), reader.GetString(5),
                    Enum.Parse<ReservationStatus>(reader.GetString(6)), ReadNullableString(reader, 7),
                    ReadNullableString(reader, 8), ReadNullableString(reader, 9), ReadNullableString(reader, 10),
                    ReadNullableString(reader, 11), ReadNullableDate(reader, 12), ReadNullableDate(reader, 13),
                    ReadNullableDate(reader, 14), reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    ReadNullableDate(reader, 16), ReadNullableDate(reader, 17)),
                ReadNullableDate(reader, 18)!.Value, ReadNullableDate(reader, 19)));
        return reservations;
    }

    private static async Task InsertReservationAsync(SqliteConnection connection, SqliteTransaction transaction,
        Reservation reservation, CancellationToken cancellationToken)
    {
        var item = reservation.Item;
        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO reservations (
                account_id, external_id, network_key, network_name, borrower, title, status,
                status_label, author, material_type, pickup_library_id, pickup_library, reserved_on,
                available_on, pickup_deadline, queue_position, suspension_starts_on, suspension_ends_on,
                first_observed_on, first_available_on)
            VALUES ($account, $id, $network, $name, $borrower, $title, $status,
                $statusLabel, $author, $materialType, $pickupLibraryId, $pickupLibrary, $reservedOn,
                $availableOn, $pickupDeadline, $queuePosition, $suspensionStartsOn, $suspensionEndsOn,
                $first, $available);
            """;
        insert.Parameters.AddWithValue("$account", reservation.AccountId);
        insert.Parameters.AddWithValue("$id", item.ExternalId);
        insert.Parameters.AddWithValue("$network", reservation.NetworkKey);
        insert.Parameters.AddWithValue("$name", reservation.NetworkName);
        insert.Parameters.AddWithValue("$borrower", item.Borrower);
        insert.Parameters.AddWithValue("$title", item.Title);
        insert.Parameters.AddWithValue("$status", item.Status.ToString());
        insert.Parameters.AddWithValue("$statusLabel", (object?)item.StatusLabel ?? DBNull.Value);
        insert.Parameters.AddWithValue("$author", (object?)item.Author ?? DBNull.Value);
        insert.Parameters.AddWithValue("$materialType", (object?)item.MaterialType ?? DBNull.Value);
        insert.Parameters.AddWithValue("$pickupLibraryId", (object?)item.PickupLibraryId ?? DBNull.Value);
        insert.Parameters.AddWithValue("$pickupLibrary", (object?)item.PickupLibrary ?? DBNull.Value);
        insert.Parameters.AddWithValue("$reservedOn", item.ReservedOn is { } reserved ? FormatDate(reserved) : DBNull.Value);
        insert.Parameters.AddWithValue("$availableOn", item.AvailableOn is { } available ? FormatDate(available) : DBNull.Value);
        insert.Parameters.AddWithValue("$pickupDeadline", item.PickupDeadline is { } deadline ? FormatDate(deadline) : DBNull.Value);
        insert.Parameters.AddWithValue("$queuePosition", (object?)item.QueuePosition ?? DBNull.Value);
        insert.Parameters.AddWithValue("$suspensionStartsOn", item.SuspensionStartsOn is { } start ? FormatDate(start) : DBNull.Value);
        insert.Parameters.AddWithValue("$suspensionEndsOn", item.SuspensionEndsOn is { } end ? FormatDate(end) : DBNull.Value);
        insert.Parameters.AddWithValue("$first", FormatDate(reservation.FirstObservedOn));
        insert.Parameters.AddWithValue("$available", reservation.FirstAvailableOn is { } firstAvailable ? FormatDate(firstAvailable) : DBNull.Value);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }
}
