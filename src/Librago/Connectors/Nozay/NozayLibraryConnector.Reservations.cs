using Librago.Configuration;
using Librago.Reservations;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace Librago.Connectors.Nozay;

public sealed partial class NozayLibraryConnector
{
    private static readonly Action<ILogger, Exception?> LogUnknownStatus = LoggerMessage.Define(
        LogLevel.Warning, new EventId(1202), "An unrecognized reservation status was returned by Nozay.");
    internal static string ValidateReservationUrl(string? link)
    {
        var origin = new Uri(HomeUrl);
        if (string.IsNullOrWhiteSpace(link) || !Uri.TryCreate(origin, link, out var url) ||
            url.Scheme != origin.Scheme || url.Authority != origin.Authority ||
            !Regex.IsMatch(url.AbsolutePath, @"^/abonne/reservations/id_profil/\d+/?$"))
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay account page did not contain a valid reservation list link.");
        return url.AbsoluteUri;
    }

    private async Task<IReadOnlyList<ReservationSnapshot>> GetReservationsAsync(
        NozaySession session, LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        var page = session.Page;
        await NavigateAsync(page, AccountUrl, cancellationToken);
        var expectedCount = await ReadReservationCountAsync(page, cancellationToken);
        var links = page.Locator(".abonneFiche.reservations a[href*='/abonne/reservations/id_profil/']");
        var link = await links.CountAsync() == 0 ? null : await links.First.GetAttributeAsync("href");
        await NavigateAsync(page, ValidateReservationUrl(link), cancellationToken);
        return await ReadReservationsAsync(page, account, expectedCount, cancellationToken,
            () => LogUnknownStatus(logger, null));
    }

    internal static async Task<int> ReadReservationCountAsync(IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await page.Locator("input[name='username']").IsVisibleAsync())
            throw new LibraryConnectorException(LibraryConnectorFailureKind.Authentication,
                "Nozay authentication did not reach the account page.");
        var summary = page.Locator(".abonneFiche.reservations");
        if (await summary.CountAsync() != 1)
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay reservation summary was not found.");
        return ParseReservationCount(await summary.InnerTextAsync());
    }

    internal static int ParseReservationCount(string text)
    {
        if (Regex.IsMatch(text, @"Vous\s+n['’]avez\s+aucune\s+réservation\s+en\s+cours\.?", RegexOptions.IgnoreCase)) return 0;
        var match = Regex.Match(text, @"Vous\s+avez\s+(\d+)\s+réservation(?:s|\(s\))?\s+en\s+cours", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var count)) return count;
        throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
            "The Nozay reservation summary was not recognizable.");
    }

    internal static async Task<IReadOnlyList<ReservationSnapshot>> ReadReservationsAsync(
        IPage page, LibraryAccountOptions account, int expectedCount, CancellationToken cancellationToken,
        Action? unknownStatus = null)
    {
        if (await page.Locator("input[name='username']").IsVisibleAsync())
            throw new LibraryConnectorException(LibraryConnectorFailureKind.Authentication,
                "Nozay authentication did not reach the reservations page.");
        var table = page.Locator("table.reservations");
        if (await table.CountAsync() != 1)
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay reservation table was not found.");
        var rows = table.Locator("tbody tr");
        var rowCount = await rows.CountAsync();
        if (rowCount != expectedCount)
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay reservation count does not match the account summary.");
        var result = new List<ReservationSnapshot>();
        for (var i = 0; i < rowCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = rows.Nth(i).Locator("td");
            var values = await cells.AllTextContentsAsync();
            var href = values.Count > 8 ? await GetFirstHrefAsync(cells.Nth(8)) : null;
            result.Add(NozayReservationRowParser.Parse(values.ToArray(), href, account.Borrower,
                account.BorrowerAliases, unknownStatus));
        }
        if (result.Select(r => r.ExternalId).Distinct(StringComparer.Ordinal).Count() != result.Count)
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay reservation list contains duplicate identities.");
        return result;
    }
}
