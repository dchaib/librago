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
            step = "opening the loans page";
            await NavigateAsync(page, LoansUrl, cancellationToken);
            step = "reading the loans";
            return await ReadAllLoansAsync(page, account, cancellationToken);
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
            page.WaitForURLAsync("**/abonne/**"),
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
        CancellationToken cancellationToken)
    {
        var loansPage = await ReadLoansPageAsync(page, account, cancellationToken);
        if (loansPage.ExpectedCount is not null && loansPage.ExpectedCount != loansPage.Loans.Count)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay loans table did not contain its expected number of loans.");
        }

        return loansPage.Loans;
    }

    internal static async Task<IReadOnlyList<LoanSnapshot>> ReadAllLoansAsync(
        IPage page,
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        var pendingUrls = new Queue<Uri>([new Uri(page.Url)]);
        var visitedUrls = new HashSet<string>(StringComparer.Ordinal);
        var loans = new List<LoanSnapshot>();
        int? expectedCount = null;

        while (pendingUrls.TryDequeue(out var url))
        {
            if (!visitedUrls.Add(url.AbsoluteUri))
            {
                continue;
            }

            if (!string.Equals(page.Url, url.AbsoluteUri, StringComparison.Ordinal))
            {
                await NavigateAsync(page, url.AbsoluteUri, cancellationToken);
            }

            var loansPage = await ReadLoansPageAsync(page, account, cancellationToken);
            loans.AddRange(loansPage.Loans);
            expectedCount ??= loansPage.ExpectedCount;

            foreach (var pageUrl in await GetPaginationUrlsAsync(page, url))
            {
                pendingUrls.Enqueue(pageUrl);
            }
        }

        if (expectedCount is not null && expectedCount != loans.Count)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nozay loans pages did not contain their expected number of loans.");
        }

        return loans;
    }

    private static async Task<NozayLoansPage> ReadLoansPageAsync(
        IPage page,
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        var table = page.Locator("#borrower_loans");
        var loginStillVisible = await page.Locator("input[name='username']").IsVisibleAsync();
        if (loginStillVisible)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.Authentication,
                "Nozay authentication did not reach the account page.");
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

        var expectedCount = await GetExpectedLoanCountAsync(table);
        return new NozayLoansPage(loans, expectedCount);
    }

    private static async Task<string?> GetFirstHrefAsync(ILocator cell)
    {
        var links = cell.Locator("a");
        return await links.CountAsync() == 0
            ? null
            : await links.First.GetAttributeAsync("href");
    }

    private static async Task<int?> GetExpectedLoanCountAsync(ILocator table)
    {
        var value = await table.GetAttributeAsync("data-loans-total") ??
                    await table.GetAttributeAsync("data-total");

        return int.TryParse(value, out var count) && count >= 0 ? count : null;
    }

    private static async Task<IReadOnlyList<Uri>> GetPaginationUrlsAsync(IPage page, Uri currentUrl)
    {
        var paginationLinks = page.Locator(".pagination a[href], [rel='next'][href]");
        var urls = new List<Uri>();

        for (var index = 0; index < await paginationLinks.CountAsync(); index++)
        {
            var href = await paginationLinks.Nth(index).GetAttributeAsync("href");
            if (href is not null && Uri.TryCreate(currentUrl, href, out var url) &&
                string.Equals(url.Host, currentUrl.Host, StringComparison.OrdinalIgnoreCase) &&
                url.AbsolutePath.StartsWith("/abonne/prets", StringComparison.OrdinalIgnoreCase))
            {
                urls.Add(url);
            }
        }

        return urls;
    }

    private sealed record NozayLoansPage(
        IReadOnlyList<LoanSnapshot> Loans,
        int? ExpectedCount);
}
