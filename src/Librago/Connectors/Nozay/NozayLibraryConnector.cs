using System.Globalization;
using System.Text.RegularExpressions;
using Librago.Configuration;
using Librago.Loans;
using Microsoft.Playwright;

namespace Librago.Connectors.Nozay;

public sealed partial class NozayLibraryConnector : ILibraryConnector
{
    private const string HomeUrl = "https://www.cc-nozay-bibliotheques.fr/accueil";
    private const string AccountUrl = "https://www.cc-nozay-bibliotheques.fr/abonne/fiche/id_profil/1";
    private const string LoansUrl = "https://www.cc-nozay-bibliotheques.fr/abonne/prets/id_profil/1";

    public LibraryNetworkDescriptor Network { get; } = new(
        "Nozay",
        "nozay",
        "Réseau des bibliothèques de Nozay");

    public async Task<IReadOnlyList<LoanSnapshot>> GetLoansAsync(
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        var step = "launching the browser";
        using var playwright = await Playwright.CreateAsync();

        try
        {
            await using var browser = await playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    ChromiumSandbox = true
                });
            step = "creating the browser context";
            await using var context = await browser.NewContextAsync(
                new BrowserNewContextOptions { Locale = "fr-FR" });
            context.SetDefaultTimeout(30_000);
            var page = await context.NewPageAsync();

            step = "opening the portal";
            await NavigateAsync(page, HomeUrl, cancellationToken);
            step = "waiting for the portal challenge";
            await PassAnubisChallengeAsync(page, cancellationToken);
            step = "authenticating";
            await AuthenticateAsync(page, account, cancellationToken);
            step = "reading the account loan count";
            await NavigateAsync(page, AccountUrl, cancellationToken);
            var expectedLoanCount = await ReadLoanCountAsync(page, cancellationToken);
            step = "opening the loans page";
            await NavigateAsync(page, LoansUrl, cancellationToken);
            step = "reading the loans";
            return await ReadLoansAsync(page, account, expectedLoanCount, cancellationToken);
        }
        catch (PlaywrightException)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.Upstream,
                $"The Nozay portal failed while {step}.");
        }
    }

    private static async Task PassAnubisChallengeAsync(IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(
                await page.TitleAsync(),
                "Making sure you're not a bot!",
                StringComparison.Ordinal))
        {
            return;
        }

        await page.WaitForFunctionAsync(
            "() => document.title !== \"Making sure you're not a bot!\"",
            null,
            new PageWaitForFunctionOptions { Timeout = 30_000 });
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task AuthenticateAsync(
        IPage page,
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        var username = page.Locator("input[name='username']");
        var password = page.Locator("input[name='password']");

        await username.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await password.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        await username.FillAsync(account.Username);
        await password.FillAsync(account.Password);
        cancellationToken.ThrowIfCancellationRequested();

        await Task.WhenAll(
            page.WaitForLoadStateAsync(LoadState.DOMContentLoaded),
            password.PressAsync("Enter"));
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task NavigateAsync(
        IPage page,
        string url,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await page.GotoAsync(
            url,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        cancellationToken.ThrowIfCancellationRequested();

        if (response is not null && response.Status >= 400)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.Upstream,
                $"The Nozay portal returned HTTP status {response.Status}.");
        }
    }

    internal static async Task<IReadOnlyList<LoanSnapshot>> ReadLoansAsync(
        IPage page,
        LibraryAccountOptions account,
        int expectedLoanCount,
        CancellationToken cancellationToken)
    {
        var loansPage = await ReadLoansPageAsync(page, account, cancellationToken);
        if (expectedLoanCount != loansPage.Loans.Count)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay loans page did not contain the number of loans reported by the account page.");
        }

        return NozayLoanRowParser.AssignFallbackOccurrences(loansPage.Loans);
    }

    private static async Task<NozayLoansPage> ReadLoansPageAsync(
        IPage page,
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        var loginStillVisible = await page.Locator("input[name='username']").IsVisibleAsync();
        if (loginStillVisible)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.Authentication,
                "Nozay authentication did not reach the account page.");
        }

        var table = page.Locator("#borrower_loans");
        if (await table.CountAsync() == 0)
        {
            if (await HasExplicitEmptyLoansMessageAsync(page))
            {
                return new NozayLoansPage([]);
            }

            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay loans table was not found.");
        }

        try
        {
            await table.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
        catch (PlaywrightException)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
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
            {
                values[cellIndex] = await cells.Nth(cellIndex).InnerTextAsync();
            }

            var renewalHref = cellCount > 6
                ? await GetFirstHrefAsync(cells.Nth(6))
                : null;
            var titleHref = cellCount > 3
                ? await GetFirstHrefAsync(cells.Nth(3))
                : null;
            loans.Add(NozayLoanRowParser.Parse(
                values,
                renewalHref,
                titleHref,
                account.Borrower,
                account.BorrowerAliases));
        }

        return new NozayLoansPage(loans);
    }

    private static async Task<string?> GetFirstHrefAsync(ILocator cell)
    {
        var links = cell.Locator("a");
        return await links.CountAsync() == 0
            ? null
            : await links.First.GetAttributeAsync("href");
    }

    internal static async Task<int> ReadLoanCountAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await page.Locator("input[name='username']").IsVisibleAsync())
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.Authentication,
                "Nozay authentication did not reach the account page.");
        }

        var summary = page.Locator(".abonneFiche.prets");
        try
        {
            await summary.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
        catch (PlaywrightException)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay account page did not contain its loans summary.");
        }

        var text = await summary.InnerTextAsync();
        if (NoLoansRegex().IsMatch(text))
        {
            return 0;
        }

        var match = LoanCountRegex().Match(text);
        if (match.Success && int.TryParse(
                match.Groups["count"].Value,
                CultureInfo.InvariantCulture,
                out var count))
        {
            return count;
        }

        throw new LibraryConnectorException(
            LibraryConnectorFailureKind.UnexpectedResponse,
            "The Nozay account page did not report a recognizable number of current loans.");
    }

    private static async Task<bool> HasExplicitEmptyLoansMessageAsync(IPage page) =>
        (await page.Locator(".contenuInner > p.error").AllTextContentsAsync())
        .Any(text => string.Equals(
            Regex.Replace(text, @"\s+", " ").Trim(),
            "Pas de prêts en cours",
            StringComparison.Ordinal));

    [GeneratedRegex(@"Vous\s+n['’]avez\s+aucun\s+prêt\s+en\s+cours\.?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoLoansRegex();

    [GeneratedRegex(@"Vous\s+avez\s+(?<count>\d+)\s+prêts?\s+en\s+cours", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LoanCountRegex();

    private sealed record NozayLoansPage(IReadOnlyList<LoanSnapshot> Loans);
}
