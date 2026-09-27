using Librago.Configuration;
using Librago.Loans;
using Librago.Reservations;

namespace Librago.Connectors.Nantes;

public sealed partial class NantesLibraryConnector(ILogger<NantesLibraryConnector> logger) : ILibraryConnector
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogCaptureFailure = LoggerMessage.Define<string, string, string>(
        LogLevel.Warning, new EventId(1203), "Nantes {DataKind} retrieval failed ({Reason}; exception type {ExceptionType}).");

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
            var loans = await CaptureAsync(() => GetLoansAsync(session, account, cancellationToken), "loans", cancellationToken);
            var reservations = await CaptureAsync(() => GetReservationsAsync(session, account, cancellationToken), "reservations", cancellationToken);
            return new AccountSnapshot(loans, reservations);
        }
    }

    private async Task<ConnectorResult<T>> CaptureAsync<T>(
        Func<Task<T>> action, string dataKind, CancellationToken cancellationToken)
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
            LogCaptureFailure(logger, dataKind, GetDiagnosticReason(exception), exception.GetType().Name, null);
            return ConnectorResult<T>.Create(default, exception.FailureKind);
        }
        catch (Exception exception)
        {
            LogCaptureFailure(logger, dataKind, "UnhandledException", exception.GetType().Name, null);
            return ConnectorResult<T>.Create(default, LibraryConnectorFailureKind.UnexpectedResponse);
        }
    }

    // Only fixed diagnostic codes are logged; exception messages may contain upstream data.
    private static string GetDiagnosticReason(LibraryConnectorException exception) => exception.Message switch
    {
        "The Nantes loans response was not valid JSON in the expected format." => "InvalidLoansJson",
        "The Nantes loans response was incomplete." => "InvalidLoansEnvelope",
        "The Nantes loans response did not contain its items." => "MissingLoanItems",
        "The Nantes loans response contained duplicate identities." => "DuplicateLoanIdentities",
        "The Nantes loans response ended before all reported loans were returned." => "LoanCountMismatch",
        _ => exception.FailureKind.ToString()
    };
}
