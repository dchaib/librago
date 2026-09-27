namespace Librago.Reservations;

public enum ReservationStatus { Available, SoonAvailable, Unavailable, Suspended, Unknown }

public sealed record ReservationSnapshot(
    string ExternalId,
    string Borrower,
    string Title,
    ReservationStatus Status,
    string? StatusLabel = null,
    string? Author = null,
    string? MaterialType = null,
    string? PickupLibraryId = null,
    string? PickupLibrary = null,
    DateOnly? ReservedOn = null,
    DateOnly? AvailableOn = null,
    DateOnly? PickupDeadline = null,
    int? QueuePosition = null,
    DateOnly? SuspensionStartsOn = null,
    DateOnly? SuspensionEndsOn = null);

public sealed record Reservation(
    string AccountId,
    string NetworkKey,
    string NetworkName,
    ReservationSnapshot Item,
    DateOnly FirstObservedOn,
    DateOnly? FirstAvailableOn)
{
    public DateOnly ReservedOn => Item.ReservedOn ?? FirstObservedOn;
    public DateOnly? AvailableOn => Item.AvailableOn ?? FirstAvailableOn;
    public string PickupFilterKey => System.Text.Json.JsonSerializer.Serialize(
        new[] { NetworkKey, Item.PickupLibraryId ?? Item.PickupLibrary });
}
