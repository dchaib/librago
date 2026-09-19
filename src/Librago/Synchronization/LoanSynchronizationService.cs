using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Microsoft.Extensions.Options;

namespace Librago.Synchronization;

public sealed partial class LoanSynchronizationService(
    IOptions<LibragoOptions> options,
    LibraryConnectorResolver connectorResolver,
    LibragoDatabase database,
    TimeProvider timeProvider,
    ILogger<LoanSynchronizationService> logger)
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
                var loans = await connector.GetLoansAsync(account, cancellationToken);
                await database.ReplaceAccountLoansAsync(
                    account,
                    connector.Network,
                    loans,
                    attemptedAt,
                    cancellationToken);
                successfulAccounts++;
                LogAccountSynchronized(logger, connector.Network.Key, loans.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var failureReason = exception is LibraryConnectorException
                    ? exception.Message
                    : exception.GetType().Name;
                LogAccountFailure(logger, exception, connector.Network.Key, failureReason);
                await database.MarkAccountFailedAsync(
                    account,
                    connector.Network,
                    attemptedAt,
                    cancellationToken);
            }
        }

        var result = successfulAccounts switch
        {
            0 => SynchronizationResult.Failed,
            var count when count == accounts.Length => SynchronizationResult.Success,
            _ => SynchronizationResult.Partial
        };

        await database.SetNetworkStateAsync(
            accounts[0].Connector.Network.Key,
            accounts[0].Connector.Network.DisplayName,
            attemptedAt,
            result,
            cancellationToken);
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Synchronized a {NetworkKey} account with {LoanCount} current loans.")]
    private static partial void LogAccountSynchronized(
        ILogger logger,
        string networkKey,
        int loanCount);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Synchronization failed for a {NetworkKey} account ({FailureReason}).")]
    private static partial void LogAccountFailure(
        ILogger logger,
        Exception exception,
        string networkKey,
        string failureReason);

    private sealed record ConfiguredAccount(
        LibraryAccountOptions Account,
        ILibraryConnector Connector);
}
