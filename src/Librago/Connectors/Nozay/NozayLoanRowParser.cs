using System.Globalization;
using System.Text.RegularExpressions;
using Librago.Loans;

namespace Librago.Connectors.Nozay;

internal static partial class NozayLoanRowParser
{
    public static LoanSnapshot Parse(
        IReadOnlyList<string> cells,
        string? renewalHref,
        string? titleHref,
        string configuredBorrower,
        IReadOnlyDictionary<string, string>? borrowerAliases = null)
    {
        if (cells.Count < 8)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.InvalidData,
                "A Nozay loan row did not contain the expected eight columns.");
        }

        var sourceBorrower = Clean(cells[0]) ?? Clean(configuredBorrower) ?? throw new LibraryConnectorException(
            LibraryConnectorFailureKind.InvalidData,
            "A Nozay loan did not identify its borrower.");
        var borrower = ResolveBorrowerAlias(sourceBorrower, borrowerAliases);
        var title = Clean(cells[3]) ?? throw new LibraryConnectorException(
            LibraryConnectorFailureKind.InvalidData,
            "A Nozay loan did not contain a title.");
        var dueOn = ParseDueDate(cells[6]);
        var externalId = ExtractOpaqueId(renewalHref, RenewalIdRegex())
            ?? ExtractOpaqueId(titleHref, NoticeIdRegex())
            ?? ExternalLoanId.FromFallback(
                sourceBorrower,
                title,
                cells[4],
                dueOn.ToString("O", CultureInfo.InvariantCulture));

        return new LoanSnapshot(
            externalId,
            borrower,
            title,
            Clean(cells[4]),
            Clean(cells[1]),
            Clean(cells[5]),
            null,
            dueOn);
    }

    private static DateOnly ParseDueDate(string value)
    {
        var match = DateRegex().Match(value);
        if (match.Success && DateOnly.TryParseExact(
                match.Value,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        throw new LibraryConnectorException(
            LibraryConnectorFailureKind.InvalidData,
            "A Nozay loan contained an invalid return date.");
    }

    private static string? ExtractOpaqueId(string? href, Regex regex)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        var match = regex.Match(href);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : WhitespaceRegex().Replace(value, " ").Trim();

    private static string ResolveBorrowerAlias(
        string sourceBorrower,
        IReadOnlyDictionary<string, string>? borrowerAliases)
    {
        if (borrowerAliases is null)
        {
            return sourceBorrower;
        }

        foreach (var (sourceName, displayName) in borrowerAliases)
        {
            var normalizedSourceName = Clean(sourceName);
            if (normalizedSourceName is not null && string.Equals(
                    normalizedSourceName,
                    sourceBorrower,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Clean(displayName) ?? sourceBorrower;
            }
        }

        return sourceBorrower;
    }

    [GeneratedRegex(@"\b\d{2}/\d{2}/\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"/id_pret/([^/?#]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RenewalIdRegex();

    [GeneratedRegex(@"/id/([^/?#]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoticeIdRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
