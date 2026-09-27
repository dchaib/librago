using System.Text.RegularExpressions;
using Librago.Reservations;
using System.Globalization;

namespace Librago.Connectors.Nozay;

internal static class NozayReservationRowParser
{
    public static ReservationSnapshot Parse(IReadOnlyList<string> cells, string? deletionHref,
        string configuredBorrower, IReadOnlyDictionary<string, string>? aliases = null, Action? unknownStatus = null)
    {
        if (cells.Count < 9) throw Invalid();
        var match = Regex.Match(deletionHref ?? "", @"/id_delete/([^/?#]+)", RegexOptions.CultureInvariant);
        if (!match.Success) throw Invalid();
        var borrower = Clean(cells[0]) ?? Clean(configuredBorrower) ?? throw Invalid();
        foreach (var alias in aliases ?? new Dictionary<string, string>())
            if (string.Equals(Clean(alias.Key), borrower, StringComparison.OrdinalIgnoreCase))
            {
                borrower = Clean(alias.Value) ?? borrower;
                break;
            }
        var label = Clean(cells[6]);
        var status = label switch
        {
            null => ReservationStatus.Unavailable,
            "Disponible" => ReservationStatus.Available,
            _ => ReservationStatus.Unknown
        };
        if (status == ReservationStatus.Unknown) unknownStatus?.Invoke();
        return new ReservationSnapshot("nozay-reservation:" + match.Groups[1].Value,
            borrower, Clean(cells[3]) ?? throw Invalid(), status, label,
            Clean(cells[4]), Clean(cells[1]), PickupLibrary: Clean(cells[5]),
            QueuePosition: ParseRank(Clean(cells[7])));
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text)
        ? null : Regex.Replace(text, @"\s+", " ").Trim();

    private static LibraryConnectorException Invalid() => new(LibraryConnectorFailureKind.InvalidData,
        "A Nozay reservation row is missing required columns, identity, title, or borrower.");

    private static int? ParseRank(string? text) => text is null ? null :
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var rank) && rank >= 0
            ? rank : throw new LibraryConnectorException(LibraryConnectorFailureKind.InvalidData,
                "A Nozay reservation contains an invalid queue position.");
}
