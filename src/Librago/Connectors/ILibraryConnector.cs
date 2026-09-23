using Librago.Configuration;
using Librago.Loans;

namespace Librago.Connectors;

public interface ILibraryConnector
{
    LibraryNetworkDescriptor Network { get; }

    Task<IReadOnlyList<LoanSnapshot>> GetLoansAsync(
        LibraryAccountOptions account,
        CancellationToken cancellationToken);
}

public enum LibraryConnectorFailureKind
{
    Authentication,
    Upstream,
    UnexpectedResponse,
    InvalidData
}

public sealed class LibraryConnectorException : Exception
{
    public LibraryConnectorException(LibraryConnectorFailureKind failureKind, string message)
        : base(message)
    {
        FailureKind = failureKind;
    }

    public LibraryConnectorFailureKind FailureKind { get; }
}
