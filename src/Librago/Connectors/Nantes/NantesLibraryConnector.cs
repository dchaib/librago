using Librago.Configuration;
using Librago.Loans;
using Librago.Reservations;

namespace Librago.Connectors.Nantes;

public sealed partial class NantesLibraryConnector(ILogger<NantesLibraryConnector> logger) : ILibraryConnector
{
    public LibraryNetworkDescriptor Network { get; } = new(
        "Nantes",
        "nantes",
        "Bibliothèque municipale de Nantes");

    public async Task<AccountSnapshot> GetAccountSnapshotAsync(
        LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        NantesSession session;
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

        using (session.Client)
        {
            var loans = await CaptureAsync(() => GetLoansAsync(session, account, cancellationToken), cancellationToken);
            var reservations = await CaptureAsync(() => GetReservationsAsync(session, account, cancellationToken), cancellationToken);
            return new AccountSnapshot(loans, reservations);
        }
    }

    private static async Task<ConnectorResult<T>> CaptureAsync<T>(
        Func<Task<T>> action, CancellationToken cancellationToken)
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
        catch (Exception)
        {
            return ConnectorResult<T>.Create(default, LibraryConnectorFailureKind.UnexpectedResponse);
        }
    }
}
