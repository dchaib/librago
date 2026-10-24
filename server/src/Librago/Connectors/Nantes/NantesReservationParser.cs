using System.Globalization;
using System.Text.Json;
using Librago.Reservations;

namespace Librago.Connectors.Nantes;

internal static class NantesReservationParser
{
    public static IReadOnlyList<ReservationSnapshot> Parse(string json, string borrower, Action<string?>? unknownStatus = null)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("total", out var total) || !total.TryGetInt32(out var count) ||
                count < 0 || count != items.GetArrayLength())
                throw Failure(LibraryConnectorFailureKind.UnexpectedResponse);

            var result = new List<ReservationSnapshot>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    throw Failure(LibraryConnectorFailureKind.InvalidData);
                var id = Required(Text(data, "omnidexId"));
                if (!ids.Add(id)) throw Failure(LibraryConnectorFailureKind.UnexpectedResponse);
                var code = Text(data, "statusCode");
                if (IsTrue(data, "canceled") || IsTrue(data, "completed") ||
                    code is "ReservationCard.RESV_CANCELED" or "ReservationCard.RESV_COMPLETED") continue;
                var status = code switch
                {
                    "ReservationCard.RESV_AVAILABLE" => ReservationStatus.Available,
                    "ReservationCard.RESV_SOON_AVAILABLE" => ReservationStatus.SoonAvailable,
                    "ReservationCard.RESV_NOT_AVAILABLE" => ReservationStatus.Unavailable,
                    "ReservationCard.RESV_SUSPENDED" => ReservationStatus.Suspended,
                    _ => ReservationStatus.Unknown
                };
                if (status == ReservationStatus.Unknown) unknownStatus?.Invoke(code);
                var branch = data.TryGetProperty("branch", out var b) && b.ValueKind == JsonValueKind.Object ? b : default;
                result.Add(new ReservationSnapshot(id, Required(borrower), Required(Text(data, "title")), status,
                    Text(data, "statusDescription"), Text(data, "author"), Text(data, "zmatDisplay"),
                    Text(branch, "branchCode"), Text(branch, "desc"), Date(data, "resvDate"),
                    Date(data, "dateHoldForPickup"), Date(data, "expiryDate"), Rank(Text(data, "rank")),
                    Date(data, "startSuspendDate"), Date(data, "endSuspendDate")));
            }
            return result;
        }
        catch (JsonException) { throw Failure(LibraryConnectorFailureKind.UnexpectedResponse); }
        catch (InvalidOperationException) { throw Failure(LibraryConnectorFailureKind.UnexpectedResponse); }
    }

    private static bool IsTrue(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) &&
        (value.ValueKind == JsonValueKind.True ||
         value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var flag) && flag);

    private static string? Text(JsonElement data, string name)
    {
        if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty(name, out var value) ||
            value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
            throw Failure(LibraryConnectorFailureKind.InvalidData);
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string Required(string? text) => string.IsNullOrWhiteSpace(text)
        ? throw Failure(LibraryConnectorFailureKind.InvalidData) : text.Trim();

    private static DateOnly? Date(JsonElement data, string name)
    {
        var text = Text(data, name);
        if (text is null) return null;
        return DateOnly.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date : throw Failure(LibraryConnectorFailureKind.InvalidData);
    }

    internal static int? Rank(string? text) => string.IsNullOrWhiteSpace(text) ? null :
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var rank) && rank >= 0
            ? rank : throw Failure(LibraryConnectorFailureKind.InvalidData);

    private static LibraryConnectorException Failure(LibraryConnectorFailureKind kind) =>
        new(kind, "The Nantes reservations response contains an invalid envelope, identity, or field.");
}
