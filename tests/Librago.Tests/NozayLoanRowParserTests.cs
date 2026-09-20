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

        Assert.Equal("synthetic-42", loan.ExternalId);
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
}
