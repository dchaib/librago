using Librago.Reservations;

namespace Librago.Tests;

public sealed class ReservationPresentationTests
{
    [Theory]
    [InlineData(-2, "Expirée depuis 2 jours")]
    [InlineData(-1, "Expirée depuis 1 jour")]
    [InlineData(0, "Aujourd’hui")]
    [InlineData(1, "Demain")]
    [InlineData(2, "Dans 2 jours")]
    public void DeadlineIncludesTheLastPickupDay(int days, string expected)
    {
        var today = new DateOnly(2026, 9, 26);
        Assert.Equal(expected, ReservationPresentation.FormatDeadline(today.AddDays(days), today));
    }

    [Fact]
    public void SortUsesStatusDeadlineThenSuppliedAndEstimatedDatesTogether()
    {
        var old = new DateOnly(2026, 9, 1);
        Reservation Make(string title, ReservationStatus status, DateOnly? deadline = null, DateOnly? supplied = null) =>
            new("account", "network", "Network", new ReservationSnapshot(title, "Reader", title, status,
                ReservedOn: supplied, PickupDeadline: deadline), old, null);
        var reservations = new[] {
            Make("Unknown", ReservationStatus.Unknown), Make("Suspended", ReservationStatus.Suspended),
            Make("Unavailable", ReservationStatus.Unavailable), Make("Soon", ReservationStatus.SoonAvailable),
            Make("Available missing deadline", ReservationStatus.Available),
            Make("Available later deadline", ReservationStatus.Available, old.AddDays(10)),
            Make("Estimated", ReservationStatus.Available, old.AddDays(9)),
            Make("Supplied older", ReservationStatus.Available, old.AddDays(9), old.AddDays(-1)),
            Make("Supplied newer", ReservationStatus.Available, old.AddDays(9), old.AddDays(1)) };
        Assert.Equal(["Supplied older", "Estimated", "Supplied newer", "Available later deadline",
            "Available missing deadline", "Soon", "Unavailable", "Suspended", "Unknown"],
            ReservationPresentation.Sort(reservations).Select(r => r.Item.Title));
    }
}
