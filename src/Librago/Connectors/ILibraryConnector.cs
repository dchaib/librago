using Librago.Configuration;
using Librago.Loans;
using Librago.Reservations;

namespace Librago.Connectors;

public interface ILibraryConnector
{
    LibraryNetworkDescriptor Network { get; }

    Task<AccountSnapshot> GetAccountSnapshotAsync(
        LibraryAccountOptions account,
        CancellationToken cancellationToken);
}

public sealed record AccountSnapshot(
    ConnectorResult<IReadOnlyList<LoanSnapshot>> Loans,
    ConnectorResult<IReadOnlyList<ReservationSnapshot>> Reservations)
{
    public static AccountSnapshot Failed(LibraryConnectorFailureKind failureKind) => new(
        ConnectorResult<IReadOnlyList<LoanSnapshot>>.Create(default, failureKind),
        ConnectorResult<IReadOnlyList<ReservationSnapshot>>.Create(default, failureKind));
}

public sealed class ConnectorResult<T>
{
    private ConnectorResult(T? value, LibraryConnectorFailureKind? failureKind)
    {
        Value = value;
        FailureKind = failureKind;
    }

    public T? Value { get; }

    public LibraryConnectorFailureKind? FailureKind { get; }

    public bool IsSuccess => FailureKind is null;

    internal static ConnectorResult<T> Create(T? value, LibraryConnectorFailureKind? failureKind) => new(value, failureKind);
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
