using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Microsoft.Extensions.Options;

namespace Librago.Loans;

public sealed record LoanOption(string Id, string Name);
public sealed record LoanDto(string Id, string Title, string? Author, string NetworkId, string BorrowerId, DateOnly DueDate);
public sealed record LoanSynchronizationDto(
    string NetworkId, DateTimeOffset? LastAttemptAt, DateTimeOffset? LastCompleteSuccessAt,
    string? Result, string Coverage, bool HasCurrentAccountCoverage, bool IsStale);
public sealed record LoansResponse(
    IReadOnlyList<LoanDto> Loans, IReadOnlyList<LoanOption> Networks, IReadOnlyList<LoanOption> Borrowers,
    IReadOnlyList<LoanSynchronizationDto> Synchronizations, bool HasConfiguredAccounts, string TimeZone);

public sealed class LoanQuery(
    LibragoDatabase database, IOptions<LibragoOptions> options,
    LibraryConnectorResolver resolver, TimeProvider timeProvider)
{
    public async Task<LoansResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var accounts = options.Value.Accounts.Select(a => new
        {
            a.AccountId,
            Network = resolver.Resolve(a.Network).Network
        }).ToArray();
        var snapshot = await database.ReadLoanSnapshotAsync(cancellationToken);
        var loans = snapshot.Loans.Where(l => accounts.Any(a =>
            StringComparer.OrdinalIgnoreCase.Equals(a.AccountId, l.AccountId) &&
            StringComparer.Ordinal.Equals(a.Network.Key, l.NetworkKey))).ToArray();
        var networks = accounts.Select(a => new LoanOption(a.Network.Key, a.Network.DisplayName))
            .DistinctBy(n => n.Id).OrderBy(n => n.Name, StringComparer.Ordinal).ToArray();
        var borrowers = loans.Select(l => l.Item.Borrower).Order(StringComparer.Ordinal)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new LoanOption(BorrowerId(name), name)).ToArray();
        var statuses = networks.Select(network =>
        {
            var ids = accounts.Where(a => a.Network.Key == network.Id).Select(a => a.AccountId).ToArray();
            var known = ids.Count(id => snapshot.Coverage.Any(c => c.NetworkKey == network.Id &&
                StringComparer.OrdinalIgnoreCase.Equals(c.AccountId, id)));
            var state = snapshot.NetworkStates.FirstOrDefault(s => s.NetworkKey == network.Id);
            return new LoanSynchronizationDto(network.Id, state?.LastAttemptAt.ToUniversalTime(),
                state?.LastCompleteSuccessAt?.ToUniversalTime(), state?.Result.ToString().ToLowerInvariant(),
                known == 0 ? "unavailable" : known == ids.Length ? "complete" : "partial",
                state?.CoversAccounts(ids) ?? false,
                state?.LastCompleteSuccessAt is { } last && timeProvider.GetUtcNow() - last > TimeSpan.FromHours(24));
        }).ToArray();
        return new(loans.Select(l => new LoanDto(
            StableId(l.NetworkKey, l.AccountId.ToUpperInvariant(), l.Item.ExternalId),
            l.Item.Title, l.Item.Author, l.NetworkKey, BorrowerId(l.Item.Borrower), l.Item.DueOn))
            .OrderBy(l => l.DueDate).ThenBy(l => l.Id, StringComparer.Ordinal).ToArray(),
            networks, borrowers, statuses, accounts.Length > 0, options.Value.TimeZone);
    }

    private static string BorrowerId(string name) => StableId(name.ToUpperInvariant());

    private static string StableId(params string[] parts) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parts))));
}
