namespace Librago.Loans;

public sealed record Loan(
    string AccountId,
    string ExternalId,
    string NetworkKey,
    string NetworkName,
    string Borrower,
    string Title,
    string? Author,
    string? MaterialType,
    string? Branch,
    DateOnly? BorrowedOn,
    DateOnly DueOn,
    DateTimeOffset RefreshedAt);

public sealed record LoanSnapshot(
    string ExternalId,
    string Borrower,
    string Title,
    string? Author,
    string? MaterialType,
    string? Branch,
    DateOnly? BorrowedOn,
    DateOnly DueOn);

