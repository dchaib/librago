using Librago.Configuration;
using Librago.Reservations;
using System.Text.RegularExpressions;

namespace Librago.Connectors.Nantes;

public sealed partial class NantesLibraryConnector
{
    private static readonly Action<ILogger, string, Exception?> LogUnknownStatus = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(1201), "An unrecognized reservation status was returned by Nantes ({StatusCode}).");

    private async Task<IReadOnlyList<ReservationSnapshot>> GetReservationsAsync(
        NantesSession session, LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        const string query = "?type=reservations&pageNo=1&pageSize=100&locale=fr";
        SetAuthorization(session, query);
        using var response = await session.Client.GetAsync($"/in/rest/api/accountPage{query}", cancellationToken);
        await RequireSuccessAsync(response, "Nantes reservations request", cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var reservations = NantesReservationParser.Parse(json, account.Borrower,
            code => LogUnknownStatus(logger,
                code is { Length: <= 80 } && Regex.IsMatch(code, @"^ReservationCard\.RESV_[A-Z_]+$")
                    ? code : "Unclassified", null));
        var titles = await NantesCatalogueTitles.ResolveAsync(session.Client, session.Token, session.SiteKey,
            json, "omnidexId", "volume", reservations.Select(r => r.ExternalId).ToHashSet(StringComparer.Ordinal), logger, cancellationToken);
        return reservations.Select(r => titles.TryGetValue(r.ExternalId, out var title) ? r with { Title = title } : r).ToArray();
    }
}
