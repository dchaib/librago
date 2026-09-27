using System.Globalization;
using System.Text.RegularExpressions;
using Librago.Configuration;
using Librago.Loans;
using Microsoft.Playwright;

namespace Librago.Connectors.Nozay;

public sealed partial class NozayLibraryConnector
{
    private static async Task<IReadOnlyList<LoanSnapshot>> GetLoansAsync(
        NozaySession session, LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        await NavigateAsync(session.Page, AccountUrl, cancellationToken);
        var expectedCount = await ReadLoanCountAsync(session.Page, cancellationToken);
        await NavigateAsync(session.Page, LoansUrl, cancellationToken);
        return await ReadLoansAsync(session.Page, account, expectedCount, cancellationToken);
    }

    internal static async Task<IReadOnlyList<LoanSnapshot>> ReadLoansAsync(
        IPage page, LibraryAccountOptions account, int expectedLoanCount, CancellationToken cancellationToken)
    {
        var loansPage = await ReadLoansPageAsync(page, account, cancellationToken);
        if (expectedLoanCount != loansPage.Loans.Count)
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay loans page did not contain the number of loans reported by the account page.");
        return NozayLoanRowParser.AssignFallbackOccurrences(loansPage.Loans);
    }

    private static async Task<NozayLoansPage> ReadLoansPageAsync(
        IPage page, LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        if (await page.Locator("input[name='username']").IsVisibleAsync())
            throw new LibraryConnectorException(LibraryConnectorFailureKind.Authentication,
                "Nozay authentication did not reach the account page.");
        var table = page.Locator("#borrower_loans");
        if (await table.CountAsync() == 0)
        {
            if (await HasExplicitEmptyLoansMessageAsync(page)) return new NozayLoansPage([]);
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay loans table was not found.");
        }
        try
        {
            await table.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
        catch (PlaywrightException)
        {
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay loans table was not found.");
        }

        var rows = table.Locator("tbody tr");
        var rowCount = await rows.CountAsync();
        var loans = new List<LoanSnapshot>(rowCount);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = rows.Nth(rowIndex).Locator("td");
            var cellCount = await cells.CountAsync();
            var values = new string[cellCount];
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
                values[cellIndex] = await cells.Nth(cellIndex).InnerTextAsync();
            var renewalHref = cellCount > 6 ? await GetFirstHrefAsync(cells.Nth(6)) : null;
            var titleHref = cellCount > 3 ? await GetFirstHrefAsync(cells.Nth(3)) : null;
            loans.Add(NozayLoanRowParser.Parse(values, renewalHref, titleHref,
                account.Borrower, account.BorrowerAliases));
        }
        return new NozayLoansPage(loans);
    }

    private static async Task<string?> GetFirstHrefAsync(ILocator cell)
    {
        var links = cell.Locator("a");
        return await links.CountAsync() == 0 ? null : await links.First.GetAttributeAsync("href");
    }

    internal static async Task<int> ReadLoanCountAsync(IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await page.Locator("input[name='username']").IsVisibleAsync())
            throw new LibraryConnectorException(LibraryConnectorFailureKind.Authentication,
                "Nozay authentication did not reach the account page.");
        var summary = page.Locator(".abonneFiche.prets");
        try
        {
            await summary.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
        catch (PlaywrightException)
        {
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay account page did not contain its loans summary.");
        }
        var text = await summary.InnerTextAsync();
        if (NoLoansRegex().IsMatch(text)) return 0;
        var match = LoanCountRegex().Match(text);
        if (match.Success && int.TryParse(match.Groups["count"].Value, CultureInfo.InvariantCulture, out var count))
            return count;
        throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
            "The Nozay account page did not report a recognizable number of current loans.");
    }

    private static async Task<bool> HasExplicitEmptyLoansMessageAsync(IPage page) =>
        (await page.Locator(".contenuInner > p.error").AllTextContentsAsync())
        .Any(text => string.Equals(Regex.Replace(text, @"\s+", " ").Trim(),
            "Pas de prêts en cours", StringComparison.Ordinal));

    [GeneratedRegex(@"Vous\s+n['’]avez\s+aucun\s+prêt\s+en\s+cours\.?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoLoansRegex();

    [GeneratedRegex(@"Vous\s+avez\s+(?<count>\d+)\s+prêts?\s+en\s+cours", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LoanCountRegex();

    private sealed record NozayLoansPage(IReadOnlyList<LoanSnapshot> Loans);
}
