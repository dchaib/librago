using Librago.Configuration;
using Librago.Connectors;
using Librago.Connectors.Nozay;
using Microsoft.Playwright;

namespace Librago.Tests;

public sealed class NozayLibraryConnectorTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    [Fact]
    public async Task ReadLoansRejectsTheLoginPage()
    {
        // Confirmed source marker: the Nozay authentication form uses this username field.
        var page = await NewPageAsync();
        await page.SetContentAsync("<input name=\"username\"><table id=\"borrower_loans\"></table>");

        var exception = await Assert.ThrowsAsync<LibraryConnectorException>(
            () => NozayLibraryConnector.ReadLoansAsync(page, Account(), 0, CancellationToken.None));

        Assert.Equal(LibraryConnectorFailureKind.Authentication, exception.FailureKind);
    }

    [Fact]
    public async Task ReadLoansAcceptsTheObservedExplicitEmptyState()
    {
        // Confirmed source shape, anonymized from an observed Nozay page with zero current loans.
        var page = await NewPageAsync();
        await page.SetContentAsync("""
            <div class="contenuInner"><p class="error">Pas de prêts en cours</p></div>
            """);

        var loans = await NozayLibraryConnector.ReadLoansAsync(page, Account(), 0, CancellationToken.None);

        Assert.Empty(loans);
    }

    [Fact]
    public async Task ReadLoansRejectsAMissingTableWithoutTheExplicitEmptyState()
    {
        // Synthetic invariant: an unrecognizable page must not replace known data.
        var page = await NewPageAsync();
        await page.SetContentAsync("<div class=\"contenuInner\"></div>");

        var exception = await Assert.ThrowsAsync<LibraryConnectorException>(
            () => NozayLibraryConnector.ReadLoansAsync(page, Account(), 0, CancellationToken.None));

        Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, exception.FailureKind);
    }

    [Fact]
    public async Task ReadLoanCountReadsTheObservedNonZeroAccountSummary()
    {
        // Confirmed source shape, anonymized from an observed Nozay account page.
        var page = await NewPageAsync();
        await page.SetContentAsync(AccountSummary("Vous avez 12 prêts en cours"));

        var count = await NozayLibraryConnector.ReadLoanCountAsync(page, CancellationToken.None);

        Assert.Equal(12, count);
    }

    [Fact]
    public async Task ReadLoanCountReadsTheObservedZeroLoanAccountSummary()
    {
        // Confirmed source shape, anonymized from an observed Nozay account page.
        var page = await NewPageAsync();
        await page.SetContentAsync(AccountSummary("Vous n'avez aucun prêt en cours."));

        var count = await NozayLibraryConnector.ReadLoanCountAsync(page, CancellationToken.None);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadLoansRejectsAListThatDoesNotMatchTheAccountCount()
    {
        // Synthetic invariant: an incomplete list must not replace known data.
        var page = await NewPageAsync();
        await page.SetContentAsync(Table(string.Empty, "synthetic-1"));

        var exception = await Assert.ThrowsAsync<LibraryConnectorException>(
            () => NozayLibraryConnector.ReadLoansAsync(page, Account(), 2, CancellationToken.None));

        Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, exception.FailureKind);
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private async Task<IPage> NewPageAsync() =>
        await (_browser ?? throw new InvalidOperationException("The browser is not initialized.")).NewPageAsync();

    private static LibraryAccountOptions Account() => new()
    {
        AccountId = "synthetic-nozay",
        Network = "Nozay",
        Username = "synthetic-user",
        Password = "synthetic-password"
    };

    private static string AccountSummary(string summary) =>
        $"<div class=\"abonneFiche prets\"><a href=\"/abonne/prets/id_profil/synthetic\">{summary}</a></div>";

    private static string Table(string attributes, string loanId) =>
        $"""
        <table id="borrower_loans" class="models tablesorter loans" data-emptymessage="Aucune donnée" {attributes}>
          <tbody>
            <tr>
              <td>Lecteur synthétique</td><td>Livre</td><td></td>
              <td><a href="/notice/id/{loanId}">Titre synthétique</a></td>
              <td>Auteur synthétique</td><td>Bibliothèque synthétique</td>
              <td><a href="/abonne/prolongerPret/id_profil/1/id_pret/{loanId}">18/09/2026</a></td><td></td>
            </tr>
          </tbody>
        </table>
        """;
}
