using System.Text.Json;
using Librago.Connectors;
using Librago.Connectors.Nantes;
using Librago.Connectors.Nozay;
using Librago.Reservations;

namespace Librago.Tests;

public sealed class ReservationParserTests
{
    [Theory]
    [InlineData("ReservationCard.RESV_AVAILABLE", ReservationStatus.Available)]
    [InlineData("ReservationCard.RESV_SOON_AVAILABLE", ReservationStatus.SoonAvailable)]
    [InlineData("ReservationCard.RESV_NOT_AVAILABLE", ReservationStatus.Unavailable)]
    [InlineData("ReservationCard.RESV_SUSPENDED", ReservationStatus.Suspended)]
    [InlineData("NEW_STATUS", ReservationStatus.Unknown)]
    public void NantesMapsStatusAndOptionalFields(string code, ReservationStatus expected)
    {
        var warnings = 0;
        var json = JsonSerializer.Serialize(new
        {
            total = 1,
            items = new[] { new { data = new {
            omnidexId = "synthetic-1", title = "Titre exemple", statusCode = code,
            statusDescription = "Libellé source", rank = "1", resvDate = "01/09/2026",
            dateHoldForPickup = "20/09/2026", expiryDate = "26/09/2026",
            startSuspendDate = "02/09/2026", endSuspendDate = "19/09/2026",
            branch = new { branchCode = "example", desc = "Bibliothèque exemple" }
        } } }
        });
        var item = Assert.Single(NantesReservationParser.Parse(json, "Lecteur exemple", _ => warnings++));
        Assert.Equal(expected, item.Status);
        Assert.Equal("Libellé source", item.StatusLabel);
        Assert.Equal(1, item.QueuePosition);
        Assert.Equal(new DateOnly(2026, 9, 26), item.PickupDeadline);
        Assert.Equal(new DateOnly(2026, 9, 2), item.SuspensionStartsOn);
        Assert.Equal(new DateOnly(2026, 9, 19), item.SuspensionEndsOn);
        Assert.Equal("example", item.PickupLibraryId);
        Assert.Equal(expected == ReservationStatus.Unknown ? 1 : 0, warnings);
    }

    [Fact]
    public void NantesAcceptsMissingDatesAndExcludesTerminalRecordsAfterCounting()
    {
        var items = NantesReservationParser.Parse("""
            {"total":3,"items":[
              {"data":{"omnidexId":"1","title":"Actuelle","statusCode":"unknown"}},
              {"data":{"omnidexId":"2","canceled":true}},
              {"data":{"omnidexId":"3","statusCode":"ReservationCard.RESV_COMPLETED"}}
            ]}
            """, "Lecteur exemple");
        var current = Assert.Single(items);
        Assert.Null(current.ReservedOn);
        Assert.Null(current.AvailableOn);
        Assert.Null(current.PickupDeadline);
        Assert.Equal(ReservationStatus.Unknown, current.Status);
        Assert.Empty(NantesReservationParser.Parse("""{"items":[],"total":0}""", "Lecteur exemple"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"items\":[],\"total\":1}")]
    [InlineData("{\"items\":[],\"total\":-1}")]
    [InlineData("{\"items\":null,\"total\":0}")]
    [InlineData("{\"items\":[],\"total\":\"0\"}")]
    [InlineData("invalid")]
    public void NantesRejectsMalformedOrIncompleteLists(string json) =>
        Assert.Throws<LibraryConnectorException>(() => NantesReservationParser.Parse(json, "Lecteur exemple"));

    [Fact]
    public void NantesRejectsDuplicateIdentitiesAndInvalidDates()
    {
        Assert.Throws<LibraryConnectorException>(() => NantesReservationParser.Parse("""
            {"total":2,"items":[{"data":{"omnidexId":"1","title":"A"}},
            {"data":{"omnidexId":"1","title":"B"}}]}
            """, "Lecteur exemple"));
        Assert.Throws<LibraryConnectorException>(() => NantesReservationParser.Parse("""
            {"total":1,"items":[{"data":{"omnidexId":"1","title":"A","expiryDate":"31/02/2026"}}]}
            """, "Lecteur exemple"));
    }

    [Theory]
    [InlineData("", ReservationStatus.Unavailable)]
    [InlineData("Disponible", ReservationStatus.Available)]
    [InlineData("En cours de traitement", ReservationStatus.Unknown)]
    public void NozayPreservesOpaqueIdentityAndConservativeStatus(string label, ReservationStatus expected)
    {
        var item = NozayReservationRowParser.Parse(
            ["  Lecteur   exemple ", "Livre", "", "Titre exemple", "Auteur", "Centre", label, "1", ""],
            "/abonne/reservations/id_profil/2/id_delete/123_456", "Fallback",
            new Dictionary<string, string> { ["lecteur exemple"] = "Alias exemple" });
        Assert.Equal("nozay-reservation:123_456", item.ExternalId);
        Assert.Equal("Alias exemple", item.Borrower);
        Assert.Equal(expected, item.Status);
        Assert.Null(item.PickupDeadline);
    }

    [Theory]
    [InlineData("Vous avez 2 réservations en cours", 2)]
    [InlineData("Vous avez 1 réservation en cours", 1)]
    [InlineData("Vous avez 2 réservation(s) en cours", 2)]
    [InlineData("Vous n’avez aucune réservation en cours.", 0)]
    public void NozayReadsReservationCounts(string text, int count) =>
        Assert.Equal(count, NozayLibraryConnector.ParseReservationCount(text));

    [Fact]
    public void NozayRejectsMissingIdentityAndForeignNavigation()
    {
        Assert.Throws<LibraryConnectorException>(() => NozayReservationRowParser.Parse(
            ["Lecteur", "", "", "Titre", "", "", "", "", ""], "/notice/id/123", ""));
        Assert.Throws<LibraryConnectorException>(() => NozayLibraryConnector.ValidateReservationUrl(
            "https://example.org/abonne/reservations/id_profil/1"));
        Assert.EndsWith("/abonne/reservations/id_profil/2",
            NozayLibraryConnector.ValidateReservationUrl("/abonne/reservations/id_profil/2"));
        Assert.Throws<LibraryConnectorException>(() => NozayLibraryConnector.ParseReservationCount("Maintenance"));
    }
}
