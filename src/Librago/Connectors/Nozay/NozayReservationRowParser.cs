using System.Text.RegularExpressions;
using Librago.Reservations;
using System.Globalization;

namespace Librago.Connectors.Nozay;

internal static class NozayReservationRowParser
{
    public static ReservationSnapshot Parse(IReadOnlyList<string> cells, string? deletionHref,
        string configuredBorrower, DateOnly observedOn,
        IReadOnlyDictionary<string, string>? aliases = null, Action? unknownStatus = null)
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
        var deadline = ParsePickupDeadline(label, observedOn);
        var status = label switch
        {
            null => ReservationStatus.Unavailable,
            "Disponible" => ReservationStatus.Available,
            _ when deadline is not null => ReservationStatus.Available,
            _ => ReservationStatus.Unknown
        };
        if (status == ReservationStatus.Unknown) unknownStatus?.Invoke();
        return new ReservationSnapshot("nozay-reservation:" + match.Groups[1].Value,
            borrower, Clean(cells[3]) ?? throw Invalid(), status, label,
            Clean(cells[4]), Clean(cells[1]), PickupLibrary: Clean(cells[5]),
            PickupDeadline: deadline, QueuePosition: ParseRank(Clean(cells[7])));
    }

    private static DateOnly? ParsePickupDeadline(string? label, DateOnly observedOn)
    {
        if (label is null || !label.StartsWith("Disponible jusqu'", StringComparison.OrdinalIgnoreCase) &&
            !label.StartsWith("Disponible jusqu’", StringComparison.OrdinalIgnoreCase)) return null;
        var match = Regex.Match(label, @"^Disponible jusqu['’]au (?<day>\d{1,2}) (?<month>\p{L}+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success) throw Invalid();
        var dayAndMonth = $"{match.Groups["day"].Value} {match.Groups["month"].Value}";
        var candidates = Enumerable.Range(observedOn.Year - 1, 3)
            .Where(year => year is >= 1 and <= 9999)
            .Select(year => DateOnly.TryParseExact($"{dayAndMonth} {year}", "d MMMM yyyy",
                CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var date)
                ? date : (DateOnly?)null)
            .Where(date => date is not null)
            .Select(date => date!.Value)
            .OrderBy(date => Math.Abs(date.DayNumber - observedOn.DayNumber))
            .ThenBy(date => date)
            .ToArray();
        return candidates.Length > 0 ? candidates[0] : throw Invalid();
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
