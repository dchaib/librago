using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Librago.Loans;
using Librago.Synchronization;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace Librago.Pages;

public sealed class IndexModel(
    LibragoDatabase database,
    IOptions<LibragoOptions> options,
    LibraryConnectorResolver connectorResolver,
    TimeProvider timeProvider) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "reseau")]
    public string? SelectedNetwork { get; set; }

    [BindProperty(SupportsGet = true, Name = "emprunteur")]
    public string? SelectedBorrower { get; set; }

    public IReadOnlyList<Loan> Loans { get; private set; } = [];

    public IReadOnlyList<SelectListItem> NetworkOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> BorrowerOptions { get; private set; } = [];

    public IReadOnlyList<NetworkStatusViewModel> NetworkStatuses { get; private set; } = [];

    public bool HasConfiguredAccounts => options.Value.Accounts.Count > 0;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SelectedNetwork) ||
        !string.IsNullOrWhiteSpace(SelectedBorrower);

    public DateOnly Today { get; private set; }

    public string FormatSynchronizationTime(DateTimeOffset timestamp)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
        return TimeZoneInfo.ConvertTime(timestamp, timeZone)
            .ToString("dd/MM/yyyy 'à' HH:mm", CultureInfo.GetCultureInfo("fr-FR"));
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
        var now = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        Today = DateOnly.FromDateTime(now.DateTime);

        var allLoans = await database.GetLoansAsync(cancellationToken);
        var storedStates = await database.GetNetworkStatesAsync(cancellationToken);
        var configuredNetworks = options.Value.Accounts
            .Select(account => connectorResolver.Resolve(account.Network).Network)
            .DistinctBy(network => network.Key, StringComparer.Ordinal)
            .OrderBy(network => network.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        NetworkOptions = allLoans
            .Select(loan => new { loan.NetworkKey, loan.NetworkName })
            .Concat(configuredNetworks.Select(network => new
            {
                NetworkKey = network.Key,
                NetworkName = network.DisplayName
            }))
            .DistinctBy(network => network.NetworkKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(network => network.NetworkName, StringComparer.CurrentCultureIgnoreCase)
            .Select(network => new SelectListItem(network.NetworkName, network.NetworkKey))
            .ToArray();

        BorrowerOptions = allLoans
            .Select(loan => loan.Borrower)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(borrower => borrower, StringComparer.CurrentCultureIgnoreCase)
            .Select(borrower => new SelectListItem(borrower, borrower))
            .ToArray();

        Loans = allLoans
            .Where(loan => string.IsNullOrWhiteSpace(SelectedNetwork) ||
                           string.Equals(
                               loan.NetworkKey,
                               SelectedNetwork,
                               StringComparison.OrdinalIgnoreCase))
            .Where(loan => string.IsNullOrWhiteSpace(SelectedBorrower) ||
                           string.Equals(
                               loan.Borrower,
                               SelectedBorrower,
                               StringComparison.OrdinalIgnoreCase))
            .ToArray();

        NetworkStatuses = configuredNetworks
            .Select(network => NetworkStatusViewModel.Create(
                network.Key,
                network.DisplayName,
                storedStates.FirstOrDefault(state => string.Equals(
                    state.NetworkKey,
                    network.Key,
                    StringComparison.Ordinal)),
                now))
            .ToArray();
    }
}

public sealed record NetworkStatusViewModel(
    string NetworkKey,
    string NetworkName,
    DateTimeOffset? LastCompleteSuccessAt,
    SynchronizationResult? Result,
    bool IsStale)
{
    public bool NeedsAttention =>
        LastCompleteSuccessAt is null || IsStale || Result is not SynchronizationResult.Success;

    public static NetworkStatusViewModel Create(
        string networkKey,
        string networkName,
        NetworkSynchronizationState? state,
        DateTimeOffset now) =>
        new(
            networkKey,
            networkName,
            state?.LastCompleteSuccessAt,
            state?.Result,
            state?.LastCompleteSuccessAt is { } lastSuccess &&
            now.ToUniversalTime() - lastSuccess.ToUniversalTime() > TimeSpan.FromHours(24));
}
