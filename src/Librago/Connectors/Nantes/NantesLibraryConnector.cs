using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Librago.Configuration;
using Librago.Loans;

namespace Librago.Connectors.Nantes;

public sealed class NantesLibraryConnector : ILibraryConnector
{
    private const string BaseUrl = "https://catalogue-bibliotheque.nantes.fr";
    private const string MicrositeId = "7e262e6b-99ca-4cc8-ae15-329af743a48d";
    private const int PageSize = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LibraryNetworkDescriptor Network { get; } = new(
        "Nantes",
        "nantes",
        "Bibliothèque municipale de Nantes");

    public async Task<IReadOnlyList<LoanSnapshot>> GetLoansAsync(
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        var cookies = new CookieContainer();
        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookies,
            AllowAutoRedirect = false
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Add("X-microsite-id", MicrositeId);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var siteKey = await GetSiteKeyAsync(client, cancellationToken);
        var token = await AuthenticateAsync(client, account, cancellationToken);
        var loans = new List<LoanSnapshot>();

        for (var pageNumber = 1; ; pageNumber++)
        {
            var query = $"?type=loans&pageNo={pageNumber}&pageSize={PageSize}&locale=fr";
            client.DefaultRequestHeaders.Remove("X-InMedia-Authorization");
            client.DefaultRequestHeaders.Add(
                "X-InMedia-Authorization",
                $"Bearer {token} {siteKey} {CalculateRequestSignature(query)}");

            using var response = await client.GetAsync(
                $"/in/rest/api/accountPage{query}",
                cancellationToken);
            await RequireSuccessAsync(response, "Nantes loans request", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = NantesLoanParser.Parse(json, account.Borrower);
            loans.AddRange(page.Loans);

            if (loans.Count >= page.Total || page.Loans.Count == 0)
            {
                if (loans.Count != page.Total)
                {
                    throw new LibraryConnectorException(
                        "The Nantes loans response ended before all reported loans were returned.");
                }

                return loans;
            }
        }
    }

    private static async Task<string> GetSiteKeyAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync("/in/rest/api/settings.json", cancellationToken);
        await RequireSuccessAsync(response, "Nantes settings request", cancellationToken);

        try
        {
            var settings = await response.Content.ReadFromJsonAsync<SettingsResponse>(
                JsonOptions,
                cancellationToken);
            return string.IsNullOrWhiteSpace(settings?.SiteKey)
                ? throw new LibraryConnectorException(
                    "The Nantes settings response did not contain a site key.")
                : settings.SiteKey;
        }
        catch (JsonException exception)
        {
            throw new LibraryConnectorException(
                "The Nantes settings response was not valid JSON in the expected format.",
                exception);
        }
    }

    private static async Task<string> AuthenticateAsync(
        HttpClient client,
        LibraryAccountOptions account,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/in/rest/api/authenticate",
            new
            {
                username = account.Username,
                password = account.Password,
                birthdate = string.Empty,
                locale = "fr",
                pin = (string?)null,
                v3Token = string.Empty
            },
            JsonOptions,
            cancellationToken);
        await RequireSuccessAsync(response, "Nantes authentication", cancellationToken);

        try
        {
            var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(
                JsonOptions,
                cancellationToken);
            return string.IsNullOrWhiteSpace(authentication?.Token)
                ? throw new LibraryConnectorException(
                    "Nantes authentication did not return a session token.")
                : authentication.Token;
        }
        catch (JsonException exception)
        {
            throw new LibraryConnectorException(
                "The Nantes authentication response was not valid JSON in the expected format.",
                exception);
        }
    }

    private static async Task RequireSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        await response.Content.LoadIntoBufferAsync(cancellationToken);
        throw new LibraryConnectorException(
            $"{operation} failed with HTTP status {(int)response.StatusCode}.");
    }

    private static string CalculateRequestSignature(string query)
    {
        var normalizedQuery = query.Normalize(NormalizationForm.FormC);
        long signature = 305419896;

        for (var index = 0; index < normalizedQuery.Length; index++)
        {
            signature += normalizedQuery[index] * (index + 1L);
        }

        return signature.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class AuthenticationResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }
    }

    private sealed class SettingsResponse
    {
        [JsonPropertyName("ckSite")]
        public string? SiteKey { get; init; }
    }
}
