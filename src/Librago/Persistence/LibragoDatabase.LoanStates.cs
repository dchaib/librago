using System.Globalization;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Synchronization;

namespace Librago.Persistence;

public sealed partial class LibragoDatabase
{
    public async Task MarkLoanAccountFailedAsync(
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


    public async Task SetLoanNetworkStateAsync(
        string networkKey,
        string networkName,
        DateTimeOffset attemptedAt,
        SynchronizationResult result,
        IReadOnlyCollection<string> accountIds,
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
                last_complete_success_account_ids,
                result)
            VALUES (
                $networkKey,
                $networkName,
                $attemptedAt,
                CASE WHEN $result = 'Success' THEN $attemptedAt ELSE NULL END,
                CASE WHEN $result = 'Success' THEN $accountIds ELSE NULL END,
                $result)
            ON CONFLICT(network_key) DO UPDATE SET
                network_name = excluded.network_name,
                last_attempt_at = excluded.last_attempt_at,
                last_complete_success_at = CASE
                    WHEN excluded.result = 'Success' THEN excluded.last_attempt_at
                    ELSE network_sync.last_complete_success_at
                END,
                last_complete_success_account_ids = CASE
                    WHEN excluded.result = 'Success' THEN excluded.last_complete_success_account_ids
                    ELSE network_sync.last_complete_success_account_ids
                END,
                result = excluded.result;
            """;
        command.Parameters.AddWithValue("$networkKey", networkKey);
        command.Parameters.AddWithValue("$networkName", networkName);
        command.Parameters.AddWithValue("$attemptedAt", FormatTimestamp(attemptedAt));
        command.Parameters.AddWithValue("$result", result.ToString());
        command.Parameters.AddWithValue("$accountIds", SerializeAccountIds(accountIds));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<NetworkSynchronizationState>> GetLoanNetworkStatesAsync(
        CancellationToken cancellationToken)
    {
        var states = new List<NetworkSynchronizationState>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                network_key,
                network_name,
                last_attempt_at,
                last_complete_success_at,
                last_complete_success_account_ids,
                result
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
                reader.IsDBNull(4)
                    ? null
                    : DeserializeAccountIds(reader.GetString(4)),
                Enum.Parse<SynchronizationResult>(reader.GetString(5))));
        }

        return states;
    }


}