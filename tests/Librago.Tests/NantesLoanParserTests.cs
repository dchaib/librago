using Librago.Connectors;
using Librago.Connectors.Nantes;

namespace Librago.Tests;

public sealed class NantesLoanParserTests
{
    [Fact]
    public void ParseMapsACompleteLoan()
    {
        const string json =
            """
            {
              "items": [
                {
                  "data": {
                    "title": "Le jardin des lucioles",
                    "author": "Camille Martin",
                    "returnDate": "21/09/2026",
                    "loanDate": "31/08/2026",
                    "documentNumber": "synthetic-document-1",
                    "isbn": "9780000000001",
                    "isbn13": "9780000000001",
                    "categoryLabel": "Livre",
                    "branch": { "branchCode": "SYN", "desc": "Médiathèque Exemple" }
                  }
                }
              ],
              "total": 1
            }
            """;

        var page = NantesLoanParser.Parse(json, "Lecteur A");

        var loan = Assert.Single(page.Loans);
        Assert.Equal(1, page.Total);
        Assert.Equal("synthetic-document-1", loan.ExternalId);
        Assert.Equal("Lecteur A", loan.Borrower);
        Assert.Equal("Le jardin des lucioles", loan.Title);
        Assert.Equal("Camille Martin", loan.Author);
        Assert.Equal("Livre", loan.MaterialType);
        Assert.Equal("Médiathèque Exemple", loan.Branch);
        Assert.Equal(new DateOnly(2026, 8, 31), loan.BorrowedOn);
        Assert.Equal(new DateOnly(2026, 9, 21), loan.DueOn);
    }

    [Fact]
    public void ParseAcceptsAnExplicitEmptyResult()
    {
        var page = NantesLoanParser.Parse("{\"items\":[],\"total\":0}", "Lecteur A");

        Assert.Empty(page.Loans);
        Assert.Equal(0, page.Total);
    }

    [Fact]
    public void ParseRejectsMissingItemsWhenTotalIsPositive()
    {
        Assert.Throws<LibraryConnectorException>(
            () => NantesLoanParser.Parse("{\"total\":1}", "Lecteur A"));
    }

    [Fact]
    public void ParseRejectsAnUnexpectedEmptyObject()
    {
        Assert.Throws<LibraryConnectorException>(
            () => NantesLoanParser.Parse("{}", "Lecteur A"));
    }
}
