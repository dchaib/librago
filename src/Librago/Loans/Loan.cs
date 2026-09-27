namespace Librago.Loans;

public sealed record Loan(
    string AccountId,
    string NetworkKey,
    string NetworkName,
    LoanSnapshot Item,
    DateOnly FirstObservedOn)
{
    public DateOnly BorrowedOn => Item.BorrowedOn ?? FirstObservedOn;
}

public sealed record LoanSnapshot(
    string ExternalId,
    string Borrower,
    string Title,
    string? Author,
    string? MaterialType,
    string? LibraryId,
    string? Library,
    DateOnly? BorrowedOn,
    DateOnly DueOn);

