using Librago.Loans;

namespace Librago.Tests;

public sealed class LoanPresentationTests
{
    private static readonly DateOnly Today = new(2026, 9, 13);

    [Theory]
    [InlineData(2026, 9, 10, "En retard de 3 jours")]
    [InlineData(2026, 9, 12, "En retard d’un jour")]
    [InlineData(2026, 9, 13, "Aujourd’hui")]
    [InlineData(2026, 9, 14, "Demain")]
    [InlineData(2026, 9, 15, "Dans 2 jours")]
    public void FormatDeadlineFormatsRelativeDates(
        int year,
        int month,
        int day,
        string expected)
    {
        Assert.Equal(expected, LoanPresentation.FormatDeadline(new DateOnly(year, month, day), Today));
    }

    [Theory]
    [InlineData(2026, 9, 12, "deadline--overdue")]
    [InlineData(2026, 9, 15, "deadline--very-close")]
    [InlineData(2026, 9, 19, "deadline--close")]
    [InlineData(2026, 9, 26, "deadline--watch")]
    [InlineData(2026, 9, 27, "deadline--normal")]
    public void SeverityClassUsesTheProductThresholds(
        int year,
        int month,
        int day,
        string expected)
    {
        Assert.Equal(expected, LoanPresentation.SeverityClass(new DateOnly(year, month, day), Today));
    }
}
