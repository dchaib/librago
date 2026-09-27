using Librago.Configuration;
using Microsoft.Playwright;

namespace Librago.Connectors.Nozay;

public sealed partial class NozayLibraryConnector
{
    private const string HomeUrl = "https://www.cc-nozay-bibliotheques.fr/accueil";
    private const string AccountUrl = "https://www.cc-nozay-bibliotheques.fr/abonne/fiche/id_profil/1";
    private const string LoansUrl = "https://www.cc-nozay-bibliotheques.fr/abonne/prets/id_profil/1";
    private static readonly Action<ILogger, Exception?> LogMissingChromium = LoggerMessage.Define(
        LogLevel.Warning, new EventId(1203),
        "The Chromium browser required by Nozay is not installed for this Playwright version. Run: pwsh src/Librago/bin/Debug/net10.0/playwright.ps1 install chromium (without --only-shell).");

    internal static void RequireChromiumExecutable(string executablePath, ILogger diagnosticLogger)
    {
        if (File.Exists(executablePath)) return;
        LogMissingChromium(diagnosticLogger, null);
        throw new LibraryConnectorException(LibraryConnectorFailureKind.Upstream,
            "The Chromium executable required by Nozay is missing.");
    }

    private async Task<NozaySession> OpenSessionAsync(
        LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        var playwright = await Playwright.CreateAsync();
        IBrowser? browser = null;
        IBrowserContext? context = null;
        try
        {
            RequireChromiumExecutable(playwright.Chromium.ExecutablePath, logger);
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                ChromiumSandbox = true
            });
            context = await browser.NewContextAsync(new BrowserNewContextOptions { Locale = "fr-FR" });
            context.SetDefaultTimeout(30_000);
            var page = await context.NewPageAsync();
            await NavigateAsync(page, HomeUrl, cancellationToken);
            await PassAnubisChallengeAsync(page, cancellationToken);
            await AuthenticateAsync(page, account, cancellationToken);
            return new NozaySession(playwright, browser, context, page);
        }
        catch
        {
            if (context is not null) await context.DisposeAsync();
            if (browser is not null) await browser.DisposeAsync();
            playwright.Dispose();
            throw;
        }
    }

    private sealed class NozaySession(
        IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page) : IAsyncDisposable
    {
        public IPage Page { get; } = page;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }

    private static async Task PassAnubisChallengeAsync(IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(await page.TitleAsync(), "Making sure you're not a bot!", StringComparison.Ordinal)) return;
        await page.WaitForFunctionAsync("() => document.title !== \"Making sure you're not a bot!\"", null,
            new PageWaitForFunctionOptions { Timeout = 30_000 });
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task AuthenticateAsync(
        IPage page, LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        var username = page.Locator("input[name='username']");
        var password = page.Locator("input[name='password']");
        await username.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await password.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await username.FillAsync(account.Username);
        await password.FillAsync(account.Password);
        cancellationToken.ThrowIfCancellationRequested();
        await Task.WhenAll(page.WaitForLoadStateAsync(LoadState.DOMContentLoaded), password.PressAsync("Enter"));
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task NavigateAsync(IPage page, string url, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        cancellationToken.ThrowIfCancellationRequested();
        if (response is not null && response.Status >= 400)
            throw new LibraryConnectorException(LibraryConnectorFailureKind.Upstream,
                $"The Nozay portal returned HTTP status {response.Status}.");
    }
}
