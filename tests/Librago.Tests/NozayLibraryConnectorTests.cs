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

    [Fact]
    public async Task LoanIdentitiesMustBeUniqueButTitlesMayRepeat()
    {
        var page = await NewPageAsync();
        var html = Table("", "synthetic-1");
        var start = html.IndexOf("<tr>", StringComparison.Ordinal);
        var end = html.IndexOf("</tr>", StringComparison.Ordinal) + 5;
        var row = html[start..end];
        await page.SetContentAsync(html.Replace(row, row + row));
        var error = await Assert.ThrowsAsync<LibraryConnectorException>(() =>
            NozayLibraryConnector.ReadLoansAsync(page, Account(), 2, CancellationToken.None));
        Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, error.FailureKind);
        await page.SetContentAsync(html.Replace(row, row + row.Replace("synthetic-1", "synthetic-2")));
        Assert.Equal(2, (await NozayLibraryConnector.ReadLoansAsync(page, Account(), 2, CancellationToken.None)).Count);
        await page.SetContentAsync(html.Replace("/id_pret/synthetic-1", ""));
        error = await Assert.ThrowsAsync<LibraryConnectorException>(() =>
            NozayLibraryConnector.ReadLoansAsync(page, Account(), 1, CancellationToken.None));
        Assert.Equal(LibraryConnectorFailureKind.InvalidData, error.FailureKind);
    }
    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    [Fact]
    public async Task ReservationsRequireTheTableAndACompleteCount()
    {
        var page = await NewPageAsync();
        await page.SetContentAsync("<table class='tablesorter reservations'><thead><tr><th>Titre</th></tr></thead><tbody></tbody></table>");
        Assert.Empty(await NozayLibraryConnector.ReadReservationsAsync(page, Account(), 0, CancellationToken.None));
        await Assert.ThrowsAsync<LibraryConnectorException>(() =>
            NozayLibraryConnector.ReadReservationsAsync(page, Account(), 1, CancellationToken.None));
        await page.SetContentAsync("<p>Aucune réservation</p>");
        await Assert.ThrowsAsync<LibraryConnectorException>(() =>
            NozayLibraryConnector.ReadReservationsAsync(page, Account(), 0, CancellationToken.None));
    }

    [Fact]
    public async Task ReservationsExtractIdentityWithoutFollowingDeletionLinks()
    {
        var page = await NewPageAsync();
        const string row = """
            <tr><td>Lecteur exemple</td><td>Livre</td><td></td><td>Titre exemple</td>
            <td>Auteur</td><td>Centre</td><td>Disponible</td><td>1</td>
            <td><a href="/abonne/reservations/id_profil/2/id_delete/123_456">Supprimer</a></td></tr>
            """;
        await page.SetContentAsync($"<table class='reservations'><tbody>{row}</tbody></table>");
        var item = Assert.Single(await NozayLibraryConnector.ReadReservationsAsync(page, Account(), 1, CancellationToken.None));
        Assert.Equal("nozay-reservation:123_456", item.ExternalId);
        Assert.Equal("about:blank", page.Url);
        await page.SetContentAsync($"<table class='reservations'><tbody>{row}{row}</tbody></table>");
        await Assert.ThrowsAsync<LibraryConnectorException>(() =>
            NozayLibraryConnector.ReadReservationsAsync(page, Account(), 2, CancellationToken.None));
        await page.SetContentAsync("<input name='username'><table class='reservations'><tbody></tbody></table>");
        var failure = await Assert.ThrowsAsync<LibraryConnectorException>(() =>
            NozayLibraryConnector.ReadReservationsAsync(page, Account(), 0, CancellationToken.None));
        Assert.Equal(LibraryConnectorFailureKind.Authentication, failure.FailureKind);
    }

    public async ValueTask DisposeAsync()
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
