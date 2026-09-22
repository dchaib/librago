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
        Assert.Equal("00000000000000", loan.ExternalId);
        Assert.Equal("Lecteur A", loan.Borrower);
        Assert.Equal("Titre synthétique", loan.Title);
        Assert.Equal("Auteur synthétique (1970-....)", loan.Author);
        Assert.Equal("Document synthétique", loan.MaterialType);
        Assert.Equal("Médiathèque synthétique", loan.Branch);
        Assert.Equal(new DateOnly(2026, 8, 31), loan.BorrowedOn);
        Assert.Equal(new DateOnly(2026, 9, 29), loan.DueOn);
    }

    [Fact]
    public void ParseAcceptsTheAssumedEmptyItemsArray()
    {
        // Assumed source shape; awaiting observation of a Nantes account with zero current loans.
        var page = NantesLoanParser.Parse("{\"items\":[],\"total\":0}", "Lecteur A");

        Assert.Empty(page.Loans);
        Assert.Equal(0, page.Total);
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
}
