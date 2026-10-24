using System.Net;
using System.Net.Http.Json;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Loans;
using Librago.Persistence;
using Librago.Synchronization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Librago.Tests;

public sealed class LoansApiTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "librago-tests", Guid.NewGuid().ToString("N"));
    private readonly List<WebApplicationFactory<Program>> _factories = [];
    private static readonly DateTimeOffset SuccessAt = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
    private readonly Clock _clock = new() { Now = SuccessAt };
    private readonly ReadOnlyConnector _connector = new();

    [Fact]
    public async Task ContractUsesPrivateStableIdentitiesAndConfiguredAccountNetworkPairs()
    {
        var factory = Factory();
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        await Seed(database, "a", "nantes", "Reader", "First");
        await Seed(database, "b", "nantes", "reader", "Second");
        await Seed(database, "removed", "nantes", "Other", "Removed");
        var response = await factory.CreateClient().GetAsync("/api/loans", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var data = await response.Content.ReadFromJsonAsync<LoansResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(data);
        Assert.Equal(2, data.Loans.Count);
        Assert.Equal(2, data.Loans.Select(l => l.Id).Distinct().Count());
        Assert.Single(data.Borrowers);
        Assert.All(data.Loans, l => { Assert.Equal(64, l.Id.Length); Assert.Null(l.Author); });
        Assert.Equal("Europe/Paris", data.TimeZone);
        Assert.Equal("complete", Assert.Single(data.Synchronizations).Coverage);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"dueDate\":\"2026-10-25\"", json);
        Assert.DoesNotContain("source-identity", json);
        Assert.DoesNotContain("synthetic-password", json);
        var again = await Read(factory);
        Assert.Equal(data.Loans.Select(l => l.Id), again.Loans.Select(l => l.Id));
        await Seed(database, "a", "nozay", "Other", "Wrong network");
        Assert.Equal("Second", Assert.Single((await Read(factory)).Loans).Title);
        Assert.Equal("partial", Assert.Single((await Read(factory)).Synchronizations).Coverage);
        Assert.Equal(0, _connector.Calls);
    }

    [Fact]
    public async Task ChangingNetworkInvalidatesCoverageUntilNewSuccessfulSnapshot()
    {
        var factory = Factory(accountCount: 1);
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        await Seed(database, "a");
        await State(database, SynchronizationResult.Success, ["a"]);

        // A successful replacement in another network removes the old items.
        // Restoring the original configuration must not turn that into verified emptiness.
        await Seed(database, "a", network: "nozay", empty: true);
        var restored = Factory(accountCount: 1);
        var response = await Read(restored);
        Assert.Empty(response.Loans);
        Assert.Equal("unavailable", Assert.Single(response.Synchronizations).Coverage);

        await State(database, SynchronizationResult.Failed, ["a"]);
        Assert.Equal("unavailable", Assert.Single((await Read(restored)).Synchronizations).Coverage);
        await Seed(database, "a", empty: true);
        await State(database, SynchronizationResult.Success, ["a"]);
        response = await Read(restored);
        Assert.Empty(response.Loans);
        Assert.Equal("complete", Assert.Single(response.Synchronizations).Coverage);
    }

    [Fact]
    public async Task EmptySnapshotsAndFailuresPreserveCoverageAcrossRestart()
    {
        var factory = Factory();
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        Assert.Equal("unavailable", Assert.Single((await Read(factory)).Synchronizations).Coverage);
        await Seed(database, "a", empty: true);
        Assert.Equal("partial", Assert.Single((await Read(factory)).Synchronizations).Coverage);
        await Seed(database, "b", empty: true);
        await State(database, SynchronizationResult.Success, ["a", "b"]);
        await State(database, SynchronizationResult.Failed, ["a", "b"]);
        var restarted = Factory();
        var response = await Read(restarted);
        Assert.Empty(response.Loans);
        var state = Assert.Single(response.Synchronizations);
        Assert.Equal("complete", state.Coverage);
        Assert.Equal("failed", state.Result);
        Assert.Equal(SuccessAt, state.LastCompleteSuccessAt);
        Assert.True(state.HasCurrentAccountCoverage);
    }

    [Fact]
    public async Task ChangedAccountSetRequiresNewSuccessAndStalenessIsStrictlyOver24Hours()
    {
        var factory = Factory();
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        await Seed(database, "a", empty: true);
        await Seed(database, "b", empty: true);
        await State(database, SynchronizationResult.Success, ["a"]);
        Assert.False(Assert.Single((await Read(factory)).Synchronizations).HasCurrentAccountCoverage);
        await State(database, SynchronizationResult.Success, ["a", "b"]);
        _clock.Now = SuccessAt.AddHours(24);
        Assert.False(Assert.Single((await Read(factory)).Synchronizations).IsStale);
        _clock.Now = _clock.Now.AddTicks(1);
        Assert.True(Assert.Single((await Read(factory)).Synchronizations).IsStale);
        var removed = Factory(accountCount: 1);
        Assert.False(Assert.Single((await Read(removed)).Synchronizations).HasCurrentAccountCoverage);
    }

    [Fact]
    public async Task ReservationSuccessDoesNotCoverLoans()
    {
        var factory = Factory();
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        await database.SetReservationNetworkStateAsync("nantes", "Network", SuccessAt,
            SynchronizationResult.Success, ["a", "b"], TestContext.Current.CancellationToken);
        var state = Assert.Single((await Read(factory)).Synchronizations);
        Assert.Equal("unavailable", state.Coverage);
        Assert.Null(state.Result);
        Assert.Null(state.LastCompleteSuccessAt);
    }

    [Fact]
    public async Task MigrationBackfillsOnlyProvenCoverageWithoutLosingLoans()
    {
        var factory = Factory();
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        await Seed(database, "a");
        await State(database, SynchronizationResult.Success, ["b"]);
        await using (var connection = new SqliteConnection($"Data Source={Path.Combine(_directory, "test.db")};Pooling=False"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE loan_account_coverage; PRAGMA user_version = 5;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        await database.InitializeAsync(TestContext.Current.CancellationToken);
        var snapshot = await database.ReadLoanSnapshotAsync(TestContext.Current.CancellationToken);
        Assert.Single(snapshot.Loans);
        Assert.Equal("b", Assert.Single(snapshot.Coverage).AccountId);
        Assert.Equal("partial", Assert.Single((await Read(factory)).Synchronizations).Coverage);
    }

    [Fact]
    public async Task InvalidSnapshotCannotReplaceItemsOrCoverage()
    {
        var factory = Factory();
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        await Seed(database, "a");
        var network = new LibraryNetworkDescriptor("Nantes", "nantes", "Network");
        await Assert.ThrowsAsync<LibraryConnectorException>(() => database.ReplaceAccountLoansAsync(
            Account("a"), network, [Snapshot("Invalid") with { ExternalId = "" }], SuccessAt.AddHours(1),
            TestContext.Current.CancellationToken));
        var snapshot = await database.ReadLoanSnapshotAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Title", Assert.Single(snapshot.Loans).Item.Title);
        Assert.Equal(SuccessAt, Assert.Single(snapshot.Coverage).LastSuccessAt);
    }

    [Fact]
    public async Task CoverageWriteFailureRollsBackLoanReplacement()
    {
        var factory = Factory();
        var database = factory.Services.GetRequiredService<LibragoDatabase>();
        await Seed(database, "a");
        await using (var connection = new SqliteConnection($"Data Source={Path.Combine(_directory, "test.db")};Pooling=False"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TRIGGER fail_coverage BEFORE INSERT ON loan_account_coverage
                BEGIN SELECT RAISE(ABORT, 'Synthetic persistence failure'); END;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        await Assert.ThrowsAsync<SqliteException>(() => Seed(database, "a", network: "nozay", title: "Replacement"));
        var snapshot = await database.ReadLoanSnapshotAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Title", Assert.Single(snapshot.Loans).Item.Title);
        Assert.Equal(SuccessAt, Assert.Single(snapshot.Coverage).LastSuccessAt);
    }

    [Theory]
    [InlineData("Production", "/api/loans")]
    [InlineData("Staging", "/api/loans")]
    [InlineData("Development", "/api/unknown")]
    public async Task UnavailableRoutesReturnPrivateJson404(string environment, string path)
    {
        var response = await Factory(environment).CreateClient().GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task ReadErrorsReturnSanitizedProblemDetails()
    {
        var factory = Factory();
        _ = factory.Services;
        await using (var connection = new SqliteConnection($"Data Source={Path.Combine(_directory, "test.db")};Pooling=False"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE loans;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        var response = await factory.CreateClient().GetAsync("/api/loans", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("SQLite", json);
        Assert.DoesNotContain(_directory, json);
    }

    [Fact]
    public async Task NoAccountsReturnsExplicitUnconfiguredState()
    {
        var response = await Read(Factory(accountCount: 0));
        Assert.False(response.HasConfiguredAccounts);
        Assert.Empty(response.Loans);
        Assert.Empty(response.Networks);
        Assert.Empty(response.Synchronizations);
    }

    private WebApplicationFactory<Program> Factory(string environment = "Development", int accountCount = 2)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                var values = new Dictionary<string, string?>
                {
                    ["Librago:DatabasePath"] = Path.Combine(_directory, "test.db"),
                    ["Librago:TimeZone"] = "Europe/Paris"
                };
                for (var i = 0; i < accountCount; i++)
                {
                    var prefix = $"Librago:Accounts:{i}:";
                    values[prefix + "AccountId"] = i == 0 ? "a" : "b";
                    values[prefix + "Network"] = "Nantes";
                    values[prefix + "Borrower"] = "Reader";
                    values[prefix + "Username"] = "synthetic-user";
                    values[prefix + "Password"] = "synthetic-password";
                }
                configuration.AddInMemoryCollection(values);
            });
            builder.ConfigureServices(services =>
            {
                var worker = services.Single(d => d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType == typeof(SynchronizationWorker));
                services.Remove(worker);
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(_clock);
                services.RemoveAll<ILibraryConnector>();
                services.AddSingleton<ILibraryConnector>(_connector);
            });
        });
        _factories.Add(factory);
        return factory;
    }

    private static async Task<LoansResponse> Read(WebApplicationFactory<Program> factory) =>
        (await factory.CreateClient().GetFromJsonAsync<LoansResponse>("/api/loans", TestContext.Current.CancellationToken))!;

    private static LibraryAccountOptions Account(string id) => new()
    {
        AccountId = id,
        Network = "Nantes",
        Borrower = "Reader",
        Username = "synthetic-user",
        Password = "synthetic-password"
    };

    private static LoanSnapshot Snapshot(string title, string borrower = "Reader") =>
        new("source-identity", borrower, title, null, null, null, null, null, new DateOnly(2026, 10, 25));

    private static Task Seed(LibragoDatabase database, string account, string network = "nantes",
        string borrower = "Reader", string title = "Title", bool empty = false) =>
        database.ReplaceAccountLoansAsync(Account(account), new(network, network, "Network"),
            empty ? [] : [Snapshot(title, borrower)], SuccessAt, TestContext.Current.CancellationToken);

    private static Task State(LibragoDatabase database, SynchronizationResult result, string[] accounts) =>
        database.SetLoanNetworkStateAsync("nantes", "Network", SuccessAt, result, accounts, TestContext.Current.CancellationToken);

    public ValueTask InitializeAsync() { Directory.CreateDirectory(_directory); return ValueTask.CompletedTask; }
    public async ValueTask DisposeAsync()
    {
        foreach (var factory in _factories) await factory.DisposeAsync();
        Directory.Delete(_directory, recursive: true);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class ReadOnlyConnector : ILibraryConnector
    {
        public int Calls { get; private set; }
        public LibraryNetworkDescriptor Network { get; } = new("Nantes", "nantes", "Synthetic network");
        public Task<AccountSnapshot> GetAccountSnapshotAsync(LibraryAccountOptions account, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("API reads must never call a provider.");
        }
    }
}
