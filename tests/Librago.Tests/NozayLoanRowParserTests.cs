using Librago.Connectors;
using Librago.Connectors.Nozay;

namespace Librago.Tests;

public sealed class NozayLoanRowParserTests
{
    [Fact]
    public void ParseMapsTheObservedTableColumns()
    {
        string[] cells =
        [
            "Lecteur B",
            "Livre",
            string.Empty,
            "La cabane aux étoiles",
            "Alex Dupont",
            "Exemple",
            "Retour prévu le 18/09/2026",
            string.Empty
        ];

        var loan = NozayLoanRowParser.Parse(
            cells,
            "/abonne/prolongerPret/id_profil/1/id_pret/synthetic-42",
            "/recherche/viewnotice/id/987",
            string.Empty);

        Assert.Equal("nozay-loan:synthetic-42", loan.ExternalId);
        Assert.Equal("Lecteur B", loan.Borrower);
        Assert.Equal("La cabane aux étoiles", loan.Title);
        Assert.Equal("Alex Dupont", loan.Author);
        Assert.Equal("Livre", loan.MaterialType);
        Assert.Equal("Exemple", loan.Branch);
        Assert.Null(loan.BorrowedOn);
        Assert.Equal(new DateOnly(2026, 9, 18), loan.DueOn);
    }

    [Fact]
    public void ParseRejectsAnUnrecognizedDate()
    {
        string[] cells = ["Lecteur B", "Livre", "", "Titre", "Auteur", "Exemple", "inconnue", ""];

        Assert.Throws<LibraryConnectorException>(
            () => NozayLoanRowParser.Parse(cells, null, null, string.Empty));
    }

    [Fact]
    public void ParseUsesNormalizedCaseInsensitiveBorrowerAlias()
    {
        string[] cells = ["  LECTEUR   ENFANT NOM  ", "Livre", "", "Titre", "Auteur", "Exemple", "18/09/2026", ""];

        var loan = NozayLoanRowParser.Parse(
            cells,
            null,
            null,
            string.Empty,
            new Dictionary<string, string>
            {
                ["lecteur enfant nom"] = "Lecteur enfant"
            });

        Assert.Equal("Lecteur enfant", loan.Borrower);
    }

    [Fact]
    public void ParsePreservesUnmappedBorrowerAndFallbackIdWhenAliasChanges()
    {
        string[] cells = ["Lecteur Inconnu", "Livre", "", "Titre", "Auteur", "Exemple", "18/09/2026", ""];

        var unmapped = NozayLoanRowParser.Parse(cells, null, null, string.Empty);
        var firstAlias = NozayLoanRowParser.Parse(
            cells,
            null,
            null,
            string.Empty,
            new Dictionary<string, string> { ["Lecteur Inconnu"] = "Libellé un" });
        var secondAlias = NozayLoanRowParser.Parse(
            cells,
            null,
            null,
            string.Empty,
            new Dictionary<string, string> { ["Lecteur Inconnu"] = "Libellé deux" });

        Assert.Equal("Lecteur Inconnu", unmapped.Borrower);
        Assert.Equal(firstAlias.ExternalId, secondAlias.ExternalId);
        Assert.NotEqual(firstAlias.Borrower, secondAlias.Borrower);
    }

    [Fact]
    public void ParseDoesNotUseTheNoticeIdAsALoanId()
    {
        string[] cells = ["Lecteur A", "Livre", "", "Titre", "Auteur", "Exemple", "18/09/2026", ""];

        var first = NozayLoanRowParser.Parse(cells, null, "/notice/id/same-notice", string.Empty);
        var second = NozayLoanRowParser.Parse(
            ["Lecteur B", "Livre", "", "Titre", "Auteur", "Exemple", "18/09/2026", ""],
            null,
            "/notice/id/same-notice",
            string.Empty);

        Assert.StartsWith("generated:", first.ExternalId);
        Assert.NotEqual(first.ExternalId, second.ExternalId);
    }

    [Fact]
    public void AssignFallbackOccurrencesPreservesIdenticalLoans()
    {
        string[] cells = ["Lecteur A", "Livre", "", "Titre", "Auteur", "Exemple", "18/09/2026", ""];
        var loan = NozayLoanRowParser.Parse(cells, null, null, string.Empty);

        var loans = NozayLoanRowParser.AssignFallbackOccurrences([loan, loan]);

        Assert.Equal(2, loans.Count);
        Assert.NotEqual(loans[0].ExternalId, loans[1].ExternalId);
        Assert.All(loans, item => Assert.StartsWith("generated:", item.ExternalId));
    }

    [Fact]
    public void AssignFallbackOccurrencesRejectsDuplicateLoanIds()
    {
        string[] cells = ["Lecteur A", "Livre", "", "Titre", "Auteur", "Exemple", "18/09/2026", ""];
        var loan = NozayLoanRowParser.Parse(
            cells,
            "/abonne/prolongerPret/id_profil/1/id_pret/same-loan",
            null,
            string.Empty);

        var exception = Assert.Throws<LibraryConnectorException>(
            () => NozayLoanRowParser.AssignFallbackOccurrences([loan, loan]));

        Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, exception.FailureKind);
    }
}
