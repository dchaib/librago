namespace Librago.Reservations;

public static class ReservationPresentation
{
    public static string StatusLabel(ReservationStatus status) => status switch
    {
        ReservationStatus.Available => "Disponible",
        ReservationStatus.SoonAvailable => "Bientôt disponible",
        ReservationStatus.Unavailable => "Pas encore disponible",
        ReservationStatus.Suspended => "Suspendue",
        _ => "État inconnu"
    };

    public static string FormatDeadline(DateOnly deadline, DateOnly today) =>
        (deadline.DayNumber - today.DayNumber) switch
        {
            < -1 => $"Expirée depuis {today.DayNumber - deadline.DayNumber} jours",
            -1 => "Expirée depuis 1 jour",
            0 => "Aujourd’hui",
            1 => "Demain",
            var days => $"Dans {days} jours"
        };

    public static IOrderedEnumerable<Reservation> Sort(IEnumerable<Reservation> reservations) =>
        reservations.OrderBy(r => r.Item.Status)
            .ThenBy(r => r.Item.PickupDeadline ?? DateOnly.MaxValue)
            .ThenBy(r => r.ReservedOn)
            .ThenBy(r => r.Item.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Item.Borrower, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.AccountId, StringComparer.Ordinal)
            .ThenBy(r => r.Item.ExternalId, StringComparer.Ordinal);
}
