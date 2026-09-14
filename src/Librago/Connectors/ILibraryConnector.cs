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

public sealed class LibraryConnectorException : Exception
{
    public LibraryConnectorException(string message)
        : base(message)
    {
    }

    public LibraryConnectorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
