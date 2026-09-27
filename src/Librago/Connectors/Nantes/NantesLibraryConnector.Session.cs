using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Librago.Configuration;

namespace Librago.Connectors.Nantes;

public sealed partial class NantesLibraryConnector
{
    private const string BaseUrl = "https://catalogue-bibliotheque.nantes.fr";
    private const string MicrositeId = "7e262e6b-99ca-4cc8-ae15-329af743a48d";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static async Task<NantesSession> OpenSessionAsync(
        LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        try
        {
            client.DefaultRequestHeaders.Add("X-microsite-id", MicrositeId);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var siteKey = await GetSiteKeyAsync(client, cancellationToken);
            var token = await AuthenticateAsync(client, account, cancellationToken);
            return new NantesSession(client, siteKey, token);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static void SetAuthorization(NantesSession session, string query)
    {
        session.Client.DefaultRequestHeaders.Remove("X-InMedia-Authorization");
        session.Client.DefaultRequestHeaders.Add("X-InMedia-Authorization",
            $"Bearer {session.Token} {session.SiteKey} {CalculateRequestSignature(query)}");
    }

    private static async Task<string> GetSiteKeyAsync(
        HttpClient client, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync("/in/rest/api/settings.json", cancellationToken);
        await RequireSuccessAsync(response, "Nantes settings request", cancellationToken);
        try
        {
            var settings = await response.Content.ReadFromJsonAsync<SettingsResponse>(JsonOptions, cancellationToken);
            return string.IsNullOrWhiteSpace(settings?.SiteKey)
                ? throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                    "The Nantes settings response did not contain a site key.")
                : settings.SiteKey;
        }
        catch (JsonException)
        {
            throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nantes settings response was not valid JSON in the expected format.");
        }
    }

    private static async Task<string> AuthenticateAsync(
        HttpClient client, LibraryAccountOptions account, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync("/in/rest/api/authenticate", new
        {
            username = account.Username,
            password = account.Password,
            birthdate = string.Empty,
            locale = "fr",
            pin = (string?)null,
            v3Token = string.Empty
        }, JsonOptions, cancellationToken);
        await RequireSuccessAsync(response, "Nantes authentication", cancellationToken);
        try
        {
            var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions, cancellationToken);
            return string.IsNullOrWhiteSpace(authentication?.Token)
                ? throw new LibraryConnectorException(LibraryConnectorFailureKind.Authentication,
                    "Nantes authentication did not return a session token.")
                : authentication.Token;
        }
        catch (JsonException)
        {
            throw new LibraryConnectorException(LibraryConnectorFailureKind.Authentication,
                "The Nantes authentication response was not valid JSON in the expected format.");
        }
    }

    private static async Task RequireSuccessAsync(
        HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        await response.Content.LoadIntoBufferAsync(cancellationToken);
        throw new LibraryConnectorException(LibraryConnectorFailureKind.Upstream,
            $"{operation} failed with HTTP status {(int)response.StatusCode}.");
    }

    private static string CalculateRequestSignature(string query)
    {
        var normalizedQuery = query.Normalize(NormalizationForm.FormC);
        long signature = 305419896;
        for (var index = 0; index < normalizedQuery.Length; index++)
            signature += normalizedQuery[index] * (index + 1L);
        return signature.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record NantesSession(HttpClient Client, string SiteKey, string Token);

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
