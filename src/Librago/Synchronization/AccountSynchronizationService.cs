using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Microsoft.Extensions.Options;

namespace Librago.Synchronization;

public sealed partial class AccountSynchronizationService(
    IOptions<LibragoOptions> options,
    LibraryConnectorResolver connectorResolver,
    LibragoDatabase database,
    TimeProvider timeProvider,
    ILogger<AccountSynchronizationService> logger)
{
    public async Task SynchronizeAllAsync(CancellationToken cancellationToken)
    {
        var configuredAccounts = options.Value.Accounts
            .Select(account => new ConfiguredAccount(account, connectorResolver.Resolve(account.Network)))
            .ToArray();

        foreach (var network in configuredAccounts.GroupBy(
                     account => account.Connector.Network.Key, StringComparer.Ordinal))
        {
            await SynchronizeNetworkAsync(network.ToArray(), cancellationToken);
        }
    }

    private async Task SynchronizeNetworkAsync(ConfiguredAccount[] accounts, CancellationToken cancellationToken)
    {
        var attemptedAt = timeProvider.GetUtcNow();
        var successfulLoans = 0;
        var successfulReservations = 0;
        foreach (var configured in accounts)
        {
            var account = configured.Account;
            var connector = configured.Connector;
            AccountSnapshot snapshot;
            try
            {
                snapshot = await connector.GetAccountSnapshotAsync(account, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var failure = exception is LibraryConnectorException connectorException
                    ? connectorException.FailureKind : LibraryConnectorFailureKind.Upstream;
                snapshot = AccountSnapshot.Failed(failure);
            }

            var loanSucceeded = await PersistLoansAsync(configured, snapshot.Loans, attemptedAt, cancellationToken);
            var reservationSucceeded = await PersistReservationsAsync(
                configured, snapshot.Reservations, attemptedAt, cancellationToken);
            if (loanSucceeded) successfulLoans++;
            if (reservationSucceeded) successfulReservations++;
        }

        var accountIds = accounts.Select(account => account.Account.AccountId).ToArray();
        var network = accounts[0].Connector.Network;
        await database.SetNetworkStateAsync(network.Key, network.DisplayName, attemptedAt,
            GetResult(successfulLoans, accounts.Length), accountIds, cancellationToken);
        await database.SetReservationNetworkStateAsync(network.Key, network.DisplayName, attemptedAt,
            GetResult(successfulReservations, accounts.Length), accountIds, cancellationToken);
    }

    private async Task<bool> PersistLoansAsync(
        ConfiguredAccount configured,
        ConnectorResult<IReadOnlyList<Librago.Loans.LoanSnapshot>> result,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!result.IsSuccess || result.Value is null)
                throw new LibraryConnectorException(result.FailureKind ?? LibraryConnectorFailureKind.UnexpectedResponse,
                    "The connector did not return a complete loan snapshot.");
            await database.ReplaceAccountLoansAsync(configured.Account, configured.Connector.Network,
                result.Value, attemptedAt, cancellationToken);
            LogSynchronized(logger, "loans", configured.Connector.Network.Key, result.Value.Count);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failure = GetFailureKind(exception);
            LogFailure(logger, "loans", configured.Connector.Network.Key, configured.Account.AccountId, failure);
            try
            {
                await database.MarkAccountFailedAsync(configured.Account, configured.Connector.Network,
                    attemptedAt, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception stateException)
            {
                LogFailure(logger, "loan state", configured.Connector.Network.Key,
                    configured.Account.AccountId, GetFailureKind(stateException));
            }
            return false;
        }
    }

    private async Task<bool> PersistReservationsAsync(
        ConfiguredAccount configured,
        ConnectorResult<IReadOnlyList<Librago.Reservations.ReservationSnapshot>> result,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!result.IsSuccess || result.Value is null)
                throw new LibraryConnectorException(result.FailureKind ?? LibraryConnectorFailureKind.UnexpectedResponse,
                    "The connector did not return a complete reservation snapshot.");
            var observedOn = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
                attemptedAt, TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone)).DateTime);
            await database.ReplaceAccountReservationsAsync(configured.Account, configured.Connector.Network,
                result.Value, observedOn, cancellationToken);
            LogSynchronized(logger, "reservations", configured.Connector.Network.Key, result.Value.Count);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogFailure(logger, "reservations", configured.Connector.Network.Key,
                configured.Account.AccountId, GetFailureKind(exception));
            return false;
        }
    }

    private static SynchronizationResult GetResult(int successes, int total) => successes switch
    {
        0 => SynchronizationResult.Failed,
        var count when count == total => SynchronizationResult.Success,
        _ => SynchronizationResult.Partial
    };

    private static string GetFailureKind(Exception exception) => exception is LibraryConnectorException connectorException
        ? connectorException.FailureKind.ToString() : "Unexpected";

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Synchronized {DataKind} for a {NetworkKey} account ({ItemCount} items).")]
    private static partial void LogSynchronized(ILogger logger, string dataKind, string networkKey, int itemCount);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Synchronization of {DataKind} failed for account {AccountId} in {NetworkKey} ({FailureKind}).")]
    private static partial void LogFailure(ILogger logger, string dataKind, string networkKey, string accountId, string failureKind);

    private sealed record ConfiguredAccount(LibraryAccountOptions Account, ILibraryConnector Connector);
}
