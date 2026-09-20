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
        var page = await NewPageAsync();
        await page.SetContentAsync("<input name=\"username\"><table id=\"borrower_loans\"></table>");

        var exception = await Assert.ThrowsAsync<LibraryConnectorException>(
            () => NozayLibraryConnector.ReadLoansAsync(page, Account(), CancellationToken.None));

        Assert.Equal(LibraryConnectorFailureKind.Authentication, exception.FailureKind);
    }

    [Fact]
    public async Task ReadLoansAcceptsAConfirmedEmptyTable()
    {
        var page = await NewPageAsync();
        await page.SetContentAsync("""
            <table id="borrower_loans" data-loans-total="0">
              <thead><tr><th>Emprunteur</th></tr></thead>
              <tbody></tbody>
            </table>
            """);

        var loans = await NozayLibraryConnector.ReadLoansAsync(page, Account(), CancellationToken.None);

        Assert.Empty(loans);
    }

    [Fact]
    public async Task ReadLoansRejectsAnIncompleteTable()
    {
        var page = await NewPageAsync();
        await page.SetContentAsync(Table("data-loans-total=\"2\"", "synthetic-1"));

        var exception = await Assert.ThrowsAsync<LibraryConnectorException>(
            () => NozayLibraryConnector.ReadLoansAsync(page, Account(), CancellationToken.None));

        Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, exception.FailureKind);
    }

    [Fact]
    public async Task ReadAllLoansFollowsLoanPaginationAndChecksTheTotal()
    {
        var page = await NewPageAsync();
        await page.RouteAsync("**/*", async route =>
        {
            var content = route.Request.Url.Contains("page=2", StringComparison.Ordinal)
                ? Table(string.Empty, "synthetic-2")
                : $"{Table("data-loans-total=\"2\"", "synthetic-1")}<nav class=\"pagination\"><a href=\"?page=2\">Suivant</a></nav>";
            await route.FulfillAsync(new RouteFulfillOptions { Body = content, ContentType = "text/html" });
        });
        await page.GotoAsync("https://nozay.test/abonne/prets/id_profil/1");

        var loans = await NozayLibraryConnector.ReadAllLoansAsync(page, Account(), CancellationToken.None);

        Assert.Equal(2, loans.Count);
        Assert.All(loans, loan => Assert.StartsWith("synthetic-", loan.ExternalId));
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

    private static string Table(string attributes, string loanId) =>
        $"""
        <table id="borrower_loans" {attributes}>
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
