using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Microsoft.Extensions.Options;

namespace Librago.Synchronization;

public sealed partial class ReservationSynchronizationService(
    IOptions<LibragoOptions> options,
    LibraryConnectorResolver connectorResolver,
    LibragoDatabase database,
    TimeProvider timeProvider,
    ILogger<ReservationSynchronizationService> logger)
{
    public async Task SynchronizeAllAsync(CancellationToken cancellationToken)
    {
        var configuredAccounts = options.Value.Accounts
            .Select(account => new ConfiguredAccount(
                account,
                connectorResolver.Resolve(account.Network)))
            .ToArray();

        foreach (var network in configuredAccounts.GroupBy(
                     account => account.Connector.Network.Key,
                     StringComparer.Ordinal))
        {
            await SynchronizeNetworkAsync(network.ToArray(), cancellationToken);
        }
    }

    private async Task SynchronizeNetworkAsync(
        ConfiguredAccount[] accounts,
        CancellationToken cancellationToken)
    {
        var attemptedAt = timeProvider.GetUtcNow();
        var successfulAccounts = 0;

        foreach (var configuredAccount in accounts)
        {
            var account = configuredAccount.Account;
            var connector = configuredAccount.Connector;

            try
            {
                var reservations = await connector.GetReservationsAsync(account, cancellationToken);
                await database.ReplaceAccountReservationsAsync(
                    account,
                    connector.Network,
                    reservations,
                    DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(attemptedAt,
                        TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone)).DateTime),
                    cancellationToken);
                successfulAccounts++;
                LogAccountSynchronized(logger, connector.Network.Key, reservations.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var failureKind = exception is LibraryConnectorException connectorException
                    ? connectorException.FailureKind.ToString()
                    : "Unexpected";
                LogAccountFailure(
                    logger,
                    connector.Network.Key,
                    failureKind);
            }
        }

        var result = successfulAccounts switch
        {
            0 => SynchronizationResult.Failed,
            var count when count == accounts.Length => SynchronizationResult.Success,
            _ => SynchronizationResult.Partial
        };

        await database.SetReservationNetworkStateAsync(
            accounts[0].Connector.Network.Key,
            accounts[0].Connector.Network.DisplayName,
            attemptedAt,
            result,
            accounts.Select(account => account.Account.AccountId).ToArray(),
            cancellationToken);
    }

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Synchronized a {NetworkKey} account with {ReservationCount} current reservations.")]
    private static partial void LogAccountSynchronized(
        ILogger logger,
        string networkKey,
        int reservationCount);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Warning,
        Message = "Synchronization failed in {NetworkKey} ({FailureKind}).")]
    private static partial void LogAccountFailure(
        ILogger logger,
        string networkKey,
        string failureKind);

    private sealed record ConfiguredAccount(
        LibraryAccountOptions Account,
        ILibraryConnector Connector);
}

