using System.Globalization;

namespace Librago.Loans;

public static class LoanPresentation
{
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

    public static string FormatDeadline(DateOnly dueOn, DateOnly today)
    {
        var days = dueOn.DayNumber - today.DayNumber;
        return days switch
        {
            < -1 => $"En retard de {-days} jours",
            -1 => "En retard d’un jour",
            0 => "Aujourd’hui",
            1 => "Demain",
            _ => $"Dans {days} jours"
        };
    }

    public static string FormatDate(DateOnly date) =>
        date.ToString("d MMMM yyyy", FrenchCulture);

    public static string SeverityClass(DateOnly dueOn, DateOnly today)
    {
        var days = dueOn.DayNumber - today.DayNumber;
        return days switch
        {
            < 0 => "deadline--overdue",
            <= 2 => "deadline--very-close",
            <= 6 => "deadline--close",
            <= 13 => "deadline--watch",
            _ => "deadline--normal"
        };
    }
}

