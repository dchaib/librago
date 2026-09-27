using System.Globalization;
using System.Net;
using System.Text.Json;
using Librago.Connectors.Nantes;
using Librago.Connectors;
using Librago.Configuration;
using Librago.Loans;
using Librago.Reservations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Librago.Tests;

public sealed class NantesCatalogueTitlesTests
{
    [Fact]
    public async Task DistinctLoansSharingOmnidexIdReceiveTheirOwnTitleSuffixes()
    {
        var notices = 0;
        var handler = new Handler(async (request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                notices++;
                Assert.Equal("locale=fr&ids=1", await request.Content!.ReadAsStringAsync(cancellationToken));
                return Response(Notice("0000000001", "Titre catalogue"));
            }
            return Response("""
                {"total":2,"items":[
                  {"data":{"documentNumber":" 00123 ","omnidexId":"shared-notice","seqNo":1,"title":"Brut A","returnDate":"29/09/2026","hasVolume":true,"vol":"17"}},
                  {"data":{"documentNumber":456,"omnidexId":"shared-notice","seqNo":1,"title":"Brut B","returnDate":"29/09/2026","hasVolume":true,"vol":"18"}}
                ]}
                """);
        });
        using var client = Client(handler);
        var loans = await InvokeConnector<LoanSnapshot>(client, "GetLoansAsync");
        Assert.Equal(2, loans.Count);
        Assert.Equal("00123", loans[0].ExternalId);
        Assert.Equal("456", loans[1].ExternalId);
        Assert.Equal("Titre catalogue - 17", loans[0].Title);
        Assert.Equal("Titre catalogue - 18", loans[1].Title);
        Assert.Equal(1, notices);
    }

    [Fact]
    public async Task ConnectorEnrichesLoanPagesAndReservationsWhilePreservingOtherFields()
    {
        var page = 0;
        var notices = 0;
        var handler = new Handler((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                notices++;
                return Task.FromResult(Response(Notice("0000000001", "Titre catalogue")));
            }
            Assert.StartsWith("Bearer synthetic-token synthetic-site ", Assert.Single(request.Headers.GetValues("X-InMedia-Authorization")));
            if (request.RequestUri!.Query.Contains("type=reservations", StringComparison.Ordinal))
                return Task.FromResult(Response("""
                    {"total":2,"items":[
                      {"data":{"omnidexId":"reservation","seqNo":1,"title":"Brut","author":"Auteur synthétique","hasVolume":true,"volume":"3","statusCode":"ReservationCard.RESV_AVAILABLE","expiryDate":"29/09/2026"}},
                      {"data":{"omnidexId":"terminal","seqNo":99,"canceled":true}}
                    ]}
                    """));
            page++;
            Assert.Contains("pageNo=" + page.ToString(CultureInfo.InvariantCulture), request.RequestUri.Query);
            return Task.FromResult(Response(JsonSerializer.Serialize(new
            {
                total = 2,
                items = new[] { new { data = new { documentNumber = page.ToString(CultureInfo.InvariantCulture), omnidexId = "shared-notice", seqNo = 1,
                    title = "Brut", author = "Auteur synthétique", returnDate = "29/09/2026", hasVolume = true, vol = "17" } } }
            })));
        });
        using var client = Client(handler);
        var loans = await InvokeConnector<LoanSnapshot>(client, "GetLoansAsync");
        Assert.Equal(2, loans.Count);
        Assert.All(loans, l =>
        {
            Assert.Equal("Titre catalogue - 17", l.Title);
            Assert.Equal("Auteur synthétique", l.Author);
            Assert.Equal(new DateOnly(2026, 9, 29), l.DueOn);
        });
        Assert.NotEqual(loans[0].ExternalId, loans[1].ExternalId);
        var reservations = await InvokeConnector<ReservationSnapshot>(client, "GetReservationsAsync");
        var reservation = Assert.Single(reservations);
        Assert.Equal("Titre catalogue - 3", reservation.Title);
        Assert.Equal("reservation", reservation.ExternalId);
        Assert.Equal(ReservationStatus.Available, reservation.Status);
        Assert.Equal("Auteur synthétique", reservation.Author);
        Assert.Equal(new DateOnly(2026, 9, 29), reservation.PickupDeadline);
        Assert.Equal(3, notices);
    }

    [Theory]
    [InlineData("loans", true)]
    [InlineData("reservations", true)]
    [InlineData("loans", false)]
    [InlineData("reservations", false)]
    public async Task ConnectorRetainsRawTitleOnFailureAndRejectsInvalidListsBeforeEnrichment(string type, bool valid)
    {
        var notices = 0;
        var handler = new Handler((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                notices++;
                return Task.FromResult(Response("{}", HttpStatusCode.BadGateway));
            }
            return Task.FromResult(Response(JsonSerializer.Serialize(new
            {
                total = 1, items = new[] { new { data = new { documentNumber = "a", omnidexId = "a", seqNo = 1, title = "Titre brut / Auteur",
                    returnDate = valid ? "29/09/2026" : "invalid", expiryDate = valid ? "29/09/2026" : "invalid" } } }
            })));
        });
        using var client = Client(handler);
        async Task<string> FetchTitle() => type == "loans"
            ? Assert.Single(await InvokeConnector<LoanSnapshot>(client, "GetLoansAsync")).Title
            : Assert.Single(await InvokeConnector<ReservationSnapshot>(client, "GetReservationsAsync")).Title;
        if (valid)
        {
            Assert.Equal("Titre brut / Auteur", await FetchTitle());
            Assert.Equal(1, notices);
        }
        else
        {
            await Assert.ThrowsAsync<LibraryConnectorException>(FetchTitle);
            Assert.Equal(0, notices);
        }
    }

    [Fact]
    public async Task ResolvesCatalogueTitlesWithoutEditingPunctuationAndPreservesRequestHeaders()
    {
        const string json = """
            {"items":[
              {"data":{"omnidexId":"a","seqNo":12,"title":"Le zoo d'Ernald [BD] 17 / scénario Frigiel et Jean-Christophe Derrien ; dessin et couleurs Arianna Sabella"}},
              {"data":{"omnidexId":"b","seq_no":"34","title":"Saint Seiya : les chevaliers du zodiaque 3 [BD]./ / Masami Kurumada ; traduit et adapté en français par Thibaud Desbief"}},
              {"data":{"omnidexId":"c","seqNo":"12","title":"Autre exemplaire"}},
              {"data":{"omnidexId":"d","bacNo":"56","title":"Titre brut"}}
            ],"total":4}
            """;
        var handler = new Handler(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/in/rest/api/resolveBySeqNo", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer synthetic-token synthetic-site", Assert.Single(request.Headers.GetValues("X-InMedia-Authorization")));
            Assert.Equal("application/x-www-form-urlencoded", request.Content!.Headers.ContentType!.MediaType);
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            Assert.Equal("locale=fr&ids=12%2C34%2C56", body);
            return Response("""
                {"resultSet":[
                  {"id":[{"value":"p::usmarcdef_0000000034"}],"title":[{"value":"Saint Seiya : les chevaliers du zodiaque. 3 [BD]./"}]},
                  {"id":[{"value":"p::usmarcdef_56"}],"title":[{"value":"Notice synthétique"}]},
                  {"id":[{"value":"p::usmarcdef_0000000012"}],"title":[{"value":"Le zoo d'Ernald [BD]. 17"}]}
                ]}
                """);
        });
        using var client = Client(handler);
        client.DefaultRequestHeaders.Add("X-InMedia-Authorization", "original-signed-header");
        var titles = await Resolve(client, json, ["a", "b", "c", "d"]);
        Assert.Equal("Le zoo d'Ernald [BD]. 17", titles["a"]);
        Assert.Equal(titles["a"], titles["c"]);
        Assert.Equal("Saint Seiya : les chevaliers du zodiaque. 3 [BD]./", titles["b"]);
        Assert.Equal("Notice synthétique", titles["d"]);
        Assert.Equal("original-signed-header", Assert.Single(client.DefaultRequestHeaders.GetValues("X-InMedia-Authorization")));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("vol", "\"hasVolume\":true,\"vol\":\"17\"", "Titre - 17")]
    [InlineData("volume", "\"hasVolume\":true,\"volume\":\"3\"", "Titre - 3")]
    [InlineData("vol", "\"hasIssueCaption\":true,\"issueCaption\":\"N° 4\",\"hasVolume\":true,\"vol\":\"3\"", "Titre - N° 4")]
    [InlineData("vol", "\"hasIssueCaption\":true,\"issueCaption\":\"N° 4\",\"hasVolume\":{}", "Titre - N° 4")]
    [InlineData("vol", "\"hasVolume\":false,\"vol\":\"3\"", "Titre")]
    [InlineData("vol", "\"hasVolume\":true", null)]
    [InlineData("vol", "\"hasIssueCaption\":true,\"issueCaption\":\" \"", null)]
    [InlineData("vol", "\"hasVolume\":\"true\"", null)]
    [InlineData("vol", "\"hasVolume\":true,\"vol\":{}", null)]
    public async Task AppliesValidSuffixesOrRetainsSourceTitle(string field, string metadata, string? expected)
    {
        using var client = Client(new Handler((_, _) => Task.FromResult(Response(Notice("0000000001", "Titre")))));
        var json = "{\"items\":[{\"data\":{\"omnidexId\":\"a\",\"seqNo\":1," + metadata + "}}]}";
        var titles = await Resolve(client, json, ["a"], field);
        if (expected is null) Assert.Empty(titles);
        else Assert.Equal(expected, titles["a"]);
    }

    [Theory]
    [InlineData("{\"items\":[]}")]
    [InlineData("{\"items\":[{\"data\":{\"omnidexId\":\"a\",\"title\":\"Brut\"}}]}")]
    [InlineData("{\"items\":[{\"data\":{\"omnidexId\":\"excluded\",\"seqNo\":1}}]}")]
    public async Task SkipsRequestsWithoutIncludedReferences(string json)
    {
        var handler = new Handler((_, _) => throw new InvalidOperationException("No request expected"));
        using var client = Client(handler);
        Assert.Empty(await Resolve(client, json, ["a"]));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task BatchesDistinctReferencesAndRetainsSuccessfulBatches()
    {
        var json = JsonSerializer.Serialize(new { items = Enumerable.Range(1, 201).Select(i => new { data = new { omnidexId = i.ToString(CultureInfo.InvariantCulture), seqNo = i } }) });
        var batchSizes = new List<int>();
        var handler = new Handler(async (request, cancellationToken) =>
        {
            var body = Uri.UnescapeDataString(await request.Content!.ReadAsStringAsync(cancellationToken));
            var ids = body.Split("&ids=")[1].Split(',');
            batchSizes.Add(ids.Length);
            if (batchSizes.Count == 2) return Response("unusable");
            return Response(JsonSerializer.Serialize(new { resultSet = ids.Reverse().Select(id => new
            {
                id = new[] { new { value = "p::usmarcdef_" + id.PadLeft(10, '0') } },
                title = new[] { new { value = "Notice " + id } }
            }) }));
        });
        using var client = Client(handler);
        var logger = new RecordingLogger();
        var titles = await Resolve(client, json, Enumerable.Range(1, 201).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray(), logger: logger);
        Assert.Equal(new List<int> { 100, 100, 1 }, batchSizes);
        Assert.Equal(101, titles.Count);
        Assert.Equal("Notice 201", titles["201"]);
        Assert.False(titles.ContainsKey("101"));
        Assert.Equal(1, logger.Warnings);
    }

    [Theory]
    [InlineData("{\"resultSet\":[]}")]
    [InlineData("{\"resultSet\":[{\"id\":[{\"value\":\"p::usmarcdef_0000000001\"}],\"title\":[]}]}")]
    [InlineData("{\"resultSet\":[{\"id\":[{\"value\":\"p::usmarcdef_0000000001\"}],\"title\":[{\"value\":\" \"},{\"value\":\"Later title\"}]}]}")]
    [InlineData("{\"resultSet\":[{\"id\":[{\"value\":\"p::usmarcdef_0000000001\"}],\"title\":[{\"value\":123}]}]}")]
    public async Task MissingOrInvalidFirstTitleDoesNotReplaceSourceTitle(string response)
    {
        using var client = Client(new Handler((_, _) => Task.FromResult(Response(response))));
        Assert.Empty(await Resolve(client, OneEntry, ["a"]));
    }

    [Fact]
    public async Task DuplicateNoticeIsAmbiguousButOtherNoticeCanStillBeUsed()
    {
        var json = "{\"items\":[{\"data\":{\"omnidexId\":\"a\",\"seqNo\":1}},{\"data\":{\"omnidexId\":\"b\",\"seqNo\":2}}]}";
        const string response = """
            {"resultSet":[
              {"id":[{"value":"p::usmarcdef_0000000001"}],"title":[{"value":"A"}]},
              {"id":[{"value":"p::usmarcdef_0000000001"}],"title":[{"value":"B"}]},
              {"id":[{"value":"p::usmarcdef_0000000002"}],"title":[{"value":"Valid"}]}
            ]}
            """;
        using var client = Client(new Handler((_, _) => Task.FromResult(Response(response))));
        var titles = await Resolve(client, json, ["a", "b"]);
        Assert.Equal("Valid", titles["b"]);
        Assert.False(titles.ContainsKey("a"));
    }

    [Theory]
    [InlineData("http")]
    [InlineData("network")]
    [InlineData("stream")]
    [InlineData("json")]
    [InlineData("envelope")]
    [InlineData("timeout")]
    public async Task BatchFailuresAreOptionalAndLogOnlySafeMessage(string failure)
    {
        var handler = new Handler(async (_, cancellationToken) =>
        {
            if (failure == "network") throw new HttpRequestException("sensitive synthetic content");
            if (failure == "stream") throw new IOException("sensitive synthetic content");
            if (failure == "timeout") await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return failure switch
            {
                "http" => Response("sensitive synthetic content", HttpStatusCode.BadGateway),
                "json" => Response("not JSON"),
                _ => Response("{}")
            };
        });
        using var client = Client(handler);
        var logger = new RecordingLogger();
        Assert.Empty(await Resolve(client, OneEntry, ["a"], logger: logger, timeout: TimeSpan.FromMilliseconds(20)));
        Assert.Equal(1, logger.Warnings);
    }

    [Fact]
    public async Task CallerCancellationPropagatesWithoutWarning()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new Handler(async (_, token) =>
        {
            cancellation.Cancel();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Response("{}");
        });
        using var client = Client(handler);
        var logger = new RecordingLogger();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Resolve(client, OneEntry, ["a"], logger: logger, cancellationToken: cancellation.Token));
        Assert.Equal(0, logger.Warnings);
    }

    private const string OneEntry = "{\"items\":[{\"data\":{\"omnidexId\":\"a\",\"seqNo\":1}}]}";
    private static Task<IReadOnlyList<T>> InvokeConnector<T>(HttpClient client, string methodName)
    {
        var connectorType = typeof(NantesLibraryConnector);
        var sessionType = connectorType.GetNestedType("NantesSession", System.Reflection.BindingFlags.NonPublic)!;
        var session = Activator.CreateInstance(sessionType, client, "synthetic-site", "synthetic-token");
        var method = connectorType.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var connector = new NantesLibraryConnector(NullLogger<NantesLibraryConnector>.Instance);
        var account = new LibraryAccountOptions { AccountId = "synthetic", Network = "Nantes", Username = "synthetic", Password = "synthetic", Borrower = "Lecteur synthétique" };
        return (Task<IReadOnlyList<T>>)method.Invoke(method.IsStatic ? null : connector,
            [session, account, TestContext.Current.CancellationToken])!;
    }

    private static Task<IReadOnlyDictionary<string, string>> Resolve(HttpClient client, string json, string[] ids,
        string field = "vol", ILogger? logger = null, TimeSpan? timeout = null, CancellationToken? cancellationToken = null) =>
        NantesCatalogueTitles.ResolveAsync(client, "synthetic-token", "synthetic-site", json, "omnidexId", field,
            ids.ToHashSet(StringComparer.Ordinal), logger ?? NullLogger.Instance, cancellationToken ?? TestContext.Current.CancellationToken, timeout);

    private static string Notice(string id, string title) => JsonSerializer.Serialize(new
    {
        resultSet = new[] { new { id = new[] { new { value = "p::usmarcdef_" + id } }, title = new[] { new { value = title } } } }
    });
    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://synthetic.invalid") };
    private static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json) };

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return respond(request, cancellationToken);
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        public int Warnings { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Equal(LogLevel.Warning, logLevel);
            Assert.Equal(1202, eventId.Id);
            Assert.Null(exception);
            Assert.Equal("A Nantes catalogue title batch could not be enriched; source titles were retained.", formatter(state, exception));
            Warnings++;
        }
    }
}
