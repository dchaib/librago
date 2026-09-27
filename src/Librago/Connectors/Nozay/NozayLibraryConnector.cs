using Librago.Configuration;
using Microsoft.Playwright;

namespace Librago.Connectors.Nozay;

public sealed partial class NozayLibraryConnector(ILogger<NozayLibraryConnector> logger) : ILibraryConnector
{
    private static readonly Action<ILogger, string, Exception?> LogBrowserFailure = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(1204), "The Nozay browser operation failed while {Step}.");

    public LibraryNetworkDescriptor Network { get; } = new(
        "Nozay",
        "nozay",
        "Réseau des bibliothèques de Nozay");

    public async Task<AccountSnapshot> GetAccountSnapshotAsync(
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        NozaySession session;
        try
        {
            session = await OpenSessionAsync(account, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (LibraryConnectorException exception)
        {
            return AccountSnapshot.Failed(exception.FailureKind);
        }
        catch (PlaywrightException)
        {
            LogBrowserFailure(logger, "opening the account session", null);
            return AccountSnapshot.Failed(LibraryConnectorFailureKind.Upstream);
        }

        await using (session)
        {
            var loans = await CaptureAsync(() => GetLoansAsync(session, account, cancellationToken),
                "reading loans", cancellationToken);
            var reservations = await CaptureAsync(() => GetReservationsAsync(session, account, cancellationToken),
                "reading reservations", cancellationToken);
            return new AccountSnapshot(loans, reservations);
        }
    }

    private async Task<ConnectorResult<T>> CaptureAsync<T>(Func<Task<T>> action,
        string step, CancellationToken cancellationToken)
    {
        try
        {
            return ConnectorResult<T>.Create(await action(), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (LibraryConnectorException exception)
        {
            return ConnectorResult<T>.Create(default, exception.FailureKind);
        }
        catch (PlaywrightException)
        {
            LogBrowserFailure(logger, step, null);
            return ConnectorResult<T>.Create(default, LibraryConnectorFailureKind.Upstream);
        }
        catch (Exception)
        {
            return ConnectorResult<T>.Create(default, LibraryConnectorFailureKind.UnexpectedResponse);
        }
    }

}
