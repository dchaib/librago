using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Librago.Reservations;
using Librago.Synchronization;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace Librago.Pages;

public sealed class ReservationsModel(
    LibragoDatabase database,
    IOptions<LibragoOptions> options,
    LibraryConnectorResolver connectorResolver,
    TimeProvider timeProvider) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? SelectedNetwork { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedBorrower { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedPickupLibrary { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedStatus { get; set; }

    public IReadOnlyList<SelectListItem> PickupLibraryOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> StatusOptions { get; } = Enum.GetValues<ReservationStatus>()
        .Select(status => new SelectListItem(ReservationPresentation.StatusLabel(status), status.ToString())).ToArray();

    public IReadOnlyList<Reservation> Reservations { get; private set; } = [];

    public IReadOnlyList<SelectListItem> NetworkOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> BorrowerOptions { get; private set; } = [];

    public IReadOnlyList<NetworkStatusViewModel> NetworkStatuses { get; private set; } = [];

    public bool HasConfiguredAccounts => options.Value.Accounts.Count > 0;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SelectedNetwork) ||
        !string.IsNullOrWhiteSpace(SelectedBorrower) ||
        !string.IsNullOrWhiteSpace(SelectedPickupLibrary) ||
        !string.IsNullOrWhiteSpace(SelectedStatus);

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

        var configuredAccountIds = options.Value.Accounts
            .Select(account => account.AccountId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allReservations = (await database.GetReservationsAsync(cancellationToken))
            .Where(reservation => configuredAccountIds.Contains(reservation.AccountId))
            .ToArray();
        var storedStates = await database.GetReservationNetworkStatesAsync(cancellationToken);
        var configuredNetworks = options.Value.Accounts
            .Select(account => connectorResolver.Resolve(account.Network).Network)
            .DistinctBy(network => network.Key, StringComparer.Ordinal)
            .OrderBy(network => network.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        NetworkOptions = allReservations
            .Select(reservation => new { reservation.NetworkKey, reservation.NetworkName })
            .Concat(configuredNetworks.Select(network => new
            {
                NetworkKey = network.Key,
                NetworkName = network.DisplayName
            }))
            .DistinctBy(network => network.NetworkKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(network => network.NetworkName, StringComparer.CurrentCultureIgnoreCase)
            .Select(network => new SelectListItem(network.NetworkName, network.NetworkKey))
            .ToArray();

        BorrowerOptions = allReservations
            .Select(reservation => reservation.Item.Borrower)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(borrower => borrower, StringComparer.CurrentCultureIgnoreCase)
            .Select(borrower => new SelectListItem(borrower, borrower))
            .ToArray();

        var libraries = allReservations.Where(r => r.Item.PickupLibrary is not null)
            .DistinctBy(r => r.PickupFilterKey).ToArray();
        PickupLibraryOptions = libraries.OrderBy(r => r.Item.PickupLibrary, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.NetworkName, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new SelectListItem(
                libraries.Any(other => other.NetworkKey != r.NetworkKey &&
                    string.Equals(other.Item.PickupLibrary, r.Item.PickupLibrary, StringComparison.OrdinalIgnoreCase))
                    ? $"{r.Item.PickupLibrary} — {r.NetworkName}" : r.Item.PickupLibrary,
                r.PickupFilterKey)).ToArray();

        Reservations = ReservationPresentation.Sort(allReservations
            .Where(reservation => string.IsNullOrWhiteSpace(SelectedNetwork) ||
                           string.Equals(
                               reservation.NetworkKey,
                               SelectedNetwork,
                               StringComparison.OrdinalIgnoreCase))
            .Where(reservation => string.IsNullOrWhiteSpace(SelectedBorrower) ||
                           string.Equals(
                               reservation.Item.Borrower,
                               SelectedBorrower,
                               StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrWhiteSpace(SelectedPickupLibrary) || r.PickupFilterKey == SelectedPickupLibrary)
            .Where(r => string.IsNullOrWhiteSpace(SelectedStatus) || r.Item.Status.ToString() == SelectedStatus))
            .ToArray();

        NetworkStatuses = configuredNetworks
            .Select(network => NetworkStatusViewModel.Create(
                network.Key,
                network.DisplayName,
                storedStates.FirstOrDefault(state => string.Equals(
                    state.NetworkKey,
                    network.Key,
                    StringComparison.Ordinal)),
                now,
                options.Value.Accounts
                    .Where(account => string.Equals(
                        connectorResolver.Resolve(account.Network).Network.Key,
                        network.Key,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(account => account.AccountId)))
            .ToArray();
    }
}


