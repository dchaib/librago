using Librago.Configuration;
using Librago.Loans;

namespace Librago.Connectors.Nantes;

public sealed partial class NantesLibraryConnector
{
    private const int PageSize = 100;

    private static async Task<IReadOnlyList<LoanSnapshot>> GetLoansAsync(
        NantesSession session, LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        var loans = new List<LoanSnapshot>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var pageNumber = 1; ; pageNumber++)
        {
            var query = $"?type=loans&pageNo={pageNumber}&pageSize={PageSize}&locale=fr";
            SetAuthorization(session, query);
            using var response = await session.Client.GetAsync($"/in/rest/api/accountPage{query}", cancellationToken);
            await RequireSuccessAsync(response, "Nantes loans request", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = NantesLoanParser.Parse(json, account.Borrower);
            foreach (var loan in page.Loans)
                if (!ids.Add(loan.ExternalId))
                    throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                        "The Nantes loans response contained duplicate identities.");
            var retrievedCount = loans.Count + page.Loans.Count;
            if ((retrievedCount >= page.Total || page.Loans.Count == 0) && retrievedCount != page.Total)
                throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                    "The Nantes loans response ended before all reported loans were returned.");
            var titles = await NantesCatalogueTitles.ResolveAsync(session.Client, session.Token, session.SiteKey,
                json, "documentNumber", "vol", page.Loans.Select(l => l.ExternalId).ToHashSet(StringComparer.Ordinal),
                session.Logger, cancellationToken);
            foreach (var loan in page.Loans)
            {
                loans.Add(titles.TryGetValue(loan.ExternalId, out var title) ? loan with { Title = title } : loan);
            }
            if (loans.Count == page.Total) return loans;
        }
    }
}
