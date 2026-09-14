using Librago.Configuration;
using Librago.Loans;
using Microsoft.Playwright;

namespace Librago.Connectors.Nozay;

public sealed class NozayLibraryConnector : ILibraryConnector
{
    private const string HomeUrl = "https://www.cc-nozay-bibliotheques.fr/accueil";
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
                new BrowserTypeLaunchOptions { Headless = false });
            step = "creating the browser context";
            await using var context = await browser.NewContextAsync(
                new BrowserNewContextOptions { Locale = "fr-FR" });
            var page = await context.NewPageAsync();

            step = "opening the portal";
            await NavigateAsync(page, HomeUrl, cancellationToken);
            step = "waiting for the portal challenge";
            await PassAnubisChallengeAsync(page, cancellationToken);
            step = "authenticating";
            await AuthenticateAsync(page, account, cancellationToken);
            step = "opening the loans page";
            await NavigateAsync(page, LoansUrl, cancellationToken);
            step = "reading the loans";
            return await ReadLoansAsync(page, account, cancellationToken);
        }
        catch (PlaywrightException exception)
        {
            throw new LibraryConnectorException(
                $"The Nozay portal failed while {step}.",
                exception);
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
                $"The Nozay portal returned HTTP status {response.Status}.");
        }
    }

    private static async Task<IReadOnlyList<LoanSnapshot>> ReadLoansAsync(
        IPage page,
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        var table = page.Locator("#borrower_loans");
        if (await table.CountAsync() == 0)
        {
            var loginStillVisible = await page.Locator("input[name='username']").CountAsync() > 0;
            throw new LibraryConnectorException(loginStillVisible
                ? "Nozay authentication did not reach the account page."
                : "The Nozay loans table was not found.");
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
                account.Borrower));
        }

        return loans;
    }

    private static async Task<string?> GetFirstHrefAsync(ILocator cell)
    {
        var links = cell.Locator("a");
        return await links.CountAsync() == 0
            ? null
            : await links.First.GetAttributeAsync("href");
    }
}
