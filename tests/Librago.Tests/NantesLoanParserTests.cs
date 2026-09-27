using Librago.Connectors;
using Librago.Connectors.Nantes;

namespace Librago.Tests;

public sealed class NantesLoanParserTests
{
    [Fact]
    public void ParseMapsAnObservedLoanResponseShape()
    {
        // Confirmed source shape, anonymized from an observed nonempty Nantes response.
        const string json =
            """
            {
              "items": [
                {
                  "data": {
                    "author": "Auteur synthétique (1970-....)",
                    "branch": {
                      "branchCode": "SYN",
                      "desc": "Médiathèque synthétique"
                    },
                    "branchId": "00000000-0000-0000-0000-000000000001",
                    "canChangeDueDate": false,
                    "category": "SYN",
                    "categoryLabel": "Document synthétique",
                    "charge": "0.0",
                    "documentNumber": "00000000000000",
                    "omnidexId": "synthetic-loan",
                    "hasAuthor": true,
                    "hasIsbn": true,
                    "hasTitle": true,
                    "isLate": false,
                    "isRenewable": true,
                    "isbn": "0000000000",
                    "isbn10": "0000000000",
                    "isbn13": "9780000000000",
                    "loanDate": "31/08/2026",
                    "loanHour": "17:10:40",
                    "ownerBranch": {
                      "branchCode": "OWN",
                      "desc": "Médiathèque propriétaire synthétique"
                    },
                    "returnDate": "29/09/2026",
                    "returnHour": "23:58:59",
                    "seriesDisplay": [],
                    "title": "Titre synthétique",
                    "transactionLocation": "S",
                    "vol": "",
                    "zmat": "SYN",
                    "zmatDisplay": "Documents synthétiques"
                  },
                  "ilsType": "Portfolio",
                  "posInSet": 0,
                  "type": "loans"
                }
              ],
              "total": 1
            }
            """;

        var page = NantesLoanParser.Parse(json, "Lecteur A");

        var loan = Assert.Single(page.Loans);
        Assert.Equal(1, page.Total);
        Assert.Equal("synthetic-loan", loan.ExternalId);
        Assert.Equal("Lecteur A", loan.Borrower);
        Assert.Equal("Titre synthétique", loan.Title);
        Assert.Equal("Auteur synthétique (1970-....)", loan.Author);
        Assert.Equal("Document synthétique", loan.MaterialType);
        Assert.Equal("Médiathèque synthétique", loan.Library);
        Assert.Equal("SYN", loan.LibraryId);
        Assert.Equal(new DateOnly(2026, 8, 31), loan.BorrowedOn);
        Assert.Equal(new DateOnly(2026, 9, 29), loan.DueOn);
    }

    [Fact]
    public void ParseAcceptsTheObservedEmptyItemsArray()
    {
        // Confirmed source shape, observed for a Nantes account with zero current loans.
        var page = NantesLoanParser.Parse("{\"items\":[],\"total\":0}", "Lecteur A");

        Assert.Empty(page.Loans);
        Assert.Equal(0, page.Total);
    }

    [Fact]
    public void ParseRejectsMissingItemsWhenTotalIsZero()
    {
        // Synthetic invariant: an incomplete response must not clear known loans.
        Assert.Throws<LibraryConnectorException>(
            () => NantesLoanParser.Parse("{\"total\":0}", "Lecteur A"));
    }

    [Fact]
    public void ParseRejectsMissingItemsWhenTotalIsPositive()
    {
        // Synthetic invariant: a positive total without items must not replace known data.
        Assert.Throws<LibraryConnectorException>(
            () => NantesLoanParser.Parse("{\"total\":1}", "Lecteur A"));
    }

    [Fact]
    public void ParseRejectsAnUnexpectedEmptyObject()
    {
        // Synthetic invariant: an unrecognizable response must not replace known data.
        Assert.Throws<LibraryConnectorException>(
            () => NantesLoanParser.Parse("{}", "Lecteur A"));
    }
    [Theory]
    [InlineData("\"  synthetic  \"", "synthetic")]
    [InlineData("123", "123")]
    public void IdentityAcceptsStringsAndNumbers(string id, string expected)
    {
        var json = "{\"items\":[{\"data\":{\"omnidexId\":" + id + ",\"title\":\"Titre\",\"returnDate\":\"18/09/2026\"}}],\"total\":1}";
        Assert.Equal(expected, Assert.Single(NantesLoanParser.Parse(json, "Lecteur").Loans).ExternalId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\"omnidexId\":null,")]
    [InlineData("\"omnidexId\":\"   \",")]
    [InlineData("\"omnidexId\":true,")]
    [InlineData("\"omnidexId\":{},")]
    [InlineData("\"omnidexId\":[],")]
    public void IdentityIsRequiredWithoutDocumentNumberFallback(string field)
    {
        var json = "{\"items\":[{\"data\":{" + field + "\"documentNumber\":\"synthetic\",\"title\":\"Titre\",\"returnDate\":\"18/09/2026\"}}],\"total\":1}";
        var error = Assert.Throws<LibraryConnectorException>(() => NantesLoanParser.Parse(json, "Lecteur"));
        Assert.Equal(LibraryConnectorFailureKind.InvalidData, error.FailureKind);
    }
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ConnectorChecksIdentitiesAcrossPages(bool duplicate, bool samePage)
    {
        using var client = new HttpClient(new LoanPagesHandler(duplicate, samePage)) { BaseAddress = new Uri("https://synthetic.invalid") };
        var connector = typeof(NantesLibraryConnector);
        var sessionType = connector.GetNestedType("NantesSession", System.Reflection.BindingFlags.NonPublic)!;
        var session = Activator.CreateInstance(sessionType, client, "synthetic-site", "synthetic-token");
        var method = connector.GetMethod("GetLoansAsync", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var task = (Task<IReadOnlyList<Librago.Loans.LoanSnapshot>>)method.Invoke(null,
            [session, new Librago.Configuration.LibraryAccountOptions { AccountId = "synthetic", Network = "Nantes", Username = "synthetic", Password = "synthetic", Borrower = "Lecteur" }, CancellationToken.None])!;
        if (duplicate)
        {
            var error = await Assert.ThrowsAsync<LibraryConnectorException>(() => task);
            Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, error.FailureKind);
        }
        else
        {
            var loans = await task;
            Assert.Equal(2, loans.Count);
            Assert.Equal(loans[0].Title, loans[1].Title);
            Assert.NotEqual(loans[0].ExternalId, loans[1].ExternalId);
        }
    }

    private sealed class LoanPagesHandler(bool duplicate, bool samePage) : HttpMessageHandler
    {
        private int page;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            page++;
            string Item(string id) => "{\"data\":{\"omnidexId\":\"" + id + "\",\"title\":\"Titre\",\"returnDate\":\"18/09/2026\"}}";
            var items = samePage ? Item("1") + "," + Item("1") : Item(page == 1 || duplicate ? "1" : "2");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"items\":[" + items + "],\"total\":2}")
            });
        }
    }
}
