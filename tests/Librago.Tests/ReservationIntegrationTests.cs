using System.Net;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Loans;
using Librago.Reservations;
using Librago.Persistence;
using Librago.Synchronization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.DataProtection;
namespace Librago.Tests;

public sealed class ReservationIntegrationTests : IAsyncLifetime
{
    private LibragoDatabase Database => Factory.Services.GetRequiredService<LibragoDatabase>();
    private LibraryConnectorResolver Resolver => Factory.Services.GetRequiredService<LibraryConnectorResolver>();
    private static readonly DateOnly Observation = new(2026, 9, 26);

    private async Task SeedAsync(string accountId, string network, params ReservationSnapshot[] items) =>
        await Database.ReplaceAccountReservationsAsync(Account(accountId, network, "Lecteur Alpha"),
            Resolver.Resolve(network).Network, items, Observation, CancellationToken.None);

    private static ReservationSnapshot Item(string id, string title, ReservationStatus status = ReservationStatus.Available,
        string borrower = "Lecteur Alpha") => new(id, borrower, title, status,
            PickupLibrary: "Centre", PickupDeadline: Observation.AddDays(2), QueuePosition: 1);

    [Fact]
    public async Task AllFourFiltersCombineAndOptionsRemainIndependent()
    {
        var client = Factory.CreateClient();
        await SeedAsync("nantes-account", "Nantes", Item("1", "Nantes Alpha"));
        await SeedAsync("nozay-account", "Nozay", Item("2", "Nozay Alpha"),
            Item("3", "Nozay Beta", ReservationStatus.Suspended, "Lecteur Beta"));
        await SeedAsync("removed-account", "Nozay", Item("4", "Removed reservation"));
        var all = WebUtility.HtmlDecode(await client.GetStringAsync("/Reservations", TestContext.Current.CancellationToken));
        Assert.Contains("Nantes Alpha", all);
        Assert.Contains("Nozay Alpha", all);
        Assert.DoesNotContain("Removed reservation", all);
        Assert.Contains("Centre — Bibliothèque municipale de Nantes", all);
        Assert.Contains("Centre — Réseau des bibliothèques de Nozay", all);
        var libraryKey = (await Database.GetReservationsAsync(CancellationToken.None))
            .First(r => r.NetworkKey == "nozay").PickupFilterKey;
        var query = "/Reservations?SelectedNetwork=nozay&SelectedBorrower=Lecteur%20Alpha&SelectedStatus=Available&SelectedPickupLibrary=" + Uri.EscapeDataString(libraryKey);
        var combined = WebUtility.HtmlDecode(await client.GetStringAsync(query, TestContext.Current.CancellationToken));
        Assert.Contains("Nozay Alpha", combined);
        Assert.DoesNotContain("Nantes Alpha", combined);
        Assert.DoesNotContain("Nozay Beta", combined);
        Assert.Contains("Lecteur Beta", combined);
        Assert.Contains("Centre — Bibliothèque municipale de Nantes", combined);
        Assert.Contains("Date estimée d’après la première observation", combined);
        Assert.Contains("Retrait au plus tard le", combined);
        foreach (var filter in new[] { "SelectedNetwork=nantes", "SelectedBorrower=Lecteur%20Beta",
                     "SelectedStatus=Suspended", "SelectedPickupLibrary=" + Uri.EscapeDataString(libraryKey) })
        {
            var filtered = await client.GetStringAsync("/Reservations?" + filter, TestContext.Current.CancellationToken);
            Assert.Contains(filter == "SelectedNetwork=nantes" ? "Nantes Alpha" : "Nozay Beta", filtered);
            Assert.DoesNotContain(filter == "SelectedNetwork=nantes" ? "Nozay Beta" : "Nantes Alpha", filtered);
        }
        var loans = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
        Assert.DoesNotContain("selected=\"selected\"", loans);
    }

    [Fact]
    public async Task EstimatesSurviveRestartAndStatusChangesAndSuppliedDatesTakePriority()
    {
        await SeedAsync("nantes-account", "Nantes", Item("1", "Titre", ReservationStatus.Unavailable));
        var first = Assert.Single(await Database.GetReservationsAsync(CancellationToken.None));
        Assert.Null(first.AvailableOn);
        var account = Account("nantes-account", "Nantes", "Lecteur Alpha");
        var network = Resolver.Resolve("Nantes").Network;
        await Database.ReplaceAccountReservationsAsync(account, network, [Item("1", "Titre")], Observation.AddDays(3), CancellationToken.None);
        var restarted = new LibragoDatabase(Factory.Services.GetRequiredService<IOptions<LibragoOptions>>(),
            Factory.Services.GetRequiredService<IWebHostEnvironment>());
        await restarted.InitializeAsync(CancellationToken.None);
        await restarted.ReplaceAccountReservationsAsync(account, network,
            [Item("1", "Titre", ReservationStatus.Suspended)], Observation.AddDays(5), CancellationToken.None);
        var later = Assert.Single(await restarted.GetReservationsAsync(CancellationToken.None));
        Assert.Equal(Observation, later.ReservedOn);
        Assert.Equal(Observation.AddDays(3), later.AvailableOn);
        await restarted.ReplaceAccountReservationsAsync(account, network,
            [Item("1", "Titre") with { ReservedOn = Observation.AddDays(-1), AvailableOn = Observation.AddDays(2) }],
            Observation.AddDays(6), CancellationToken.None);
        var supplied = Assert.Single(await restarted.GetReservationsAsync(CancellationToken.None));
        Assert.Equal(Observation.AddDays(-1), supplied.ReservedOn);
        Assert.Equal(Observation.AddDays(2), supplied.AvailableOn);
        await restarted.ReplaceAccountReservationsAsync(account, network, [], Observation.AddDays(7), CancellationToken.None);
        Assert.Empty(await restarted.GetReservationsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RefreshFailuresPreserveReservationsAndLoanFreshnessIsIndependent()
    {
        var options = Factory.Services.GetRequiredService<IOptions<LibragoOptions>>();
        var nantes = new FakeConnector(Resolver.Resolve("Nantes").Network);
        var nozay = new FakeConnector(Resolver.Resolve("Nozay").Network);
        var resolver = new LibraryConnectorResolver([nantes, nozay]);
        var clock = new Clock(new DateTimeOffset(2026, 9, 26, 22, 30, 0, TimeSpan.Zero));
        var synchronization = new AccountSynchronizationService(options, resolver, Database, clock,
            NullLogger<AccountSynchronizationService>.Instance);
        await synchronization.SynchronizeAllAsync(CancellationToken.None);
        Assert.All(await Database.GetReservationsAsync(CancellationToken.None), r => Assert.Equal(Observation.AddDays(1), r.ReservedOn));
        clock.Now = clock.Now.AddHours(1);
        nantes.FailReservations = true;
        nozay.Title = "Updated";
        await synchronization.SynchronizeAllAsync(CancellationToken.None);
        var stored = await Database.GetReservationsAsync(CancellationToken.None);
        Assert.Contains(stored, r => r.NetworkKey == "nantes" && r.Item.Title == "Initial");
        Assert.Contains(stored, r => r.NetworkKey == "nozay" && r.Item.Title == "Updated");
        var state = (await Database.GetReservationNetworkStatesAsync(CancellationToken.None)).Single(s => s.NetworkKey == "nantes");
        Assert.Equal(SynchronizationResult.Failed, state.Result);
        Assert.Equal(clock.Now.AddHours(-1), state.LastCompleteSuccessAt);
        Assert.All(await Database.GetNetworkStatesAsync(CancellationToken.None), s => Assert.Equal(clock.Now, s.LastCompleteSuccessAt));
        nantes.FailReservations = false;
        nantes.FailLoans = true;
        nantes.Title = "Recovered";
        await synchronization.SynchronizeAllAsync(CancellationToken.None);
        Assert.Contains(await Database.GetReservationsAsync(CancellationToken.None), r => r.Item.Title == "Recovered");
    }

    [Fact]
    public async Task PartialNetworkAndEmptyStatesUseReservationFreshness()
    {
        var client = Factory.CreateClient();
        var options = Options.Create(new LibragoOptions
        {
            TimeZone = "Europe/Paris",
            Accounts = [
            Account("nantes-account", "Nantes", "Lecteur Alpha"), Account("second-account", "Nantes", "Lecteur Beta") ]
        });
        var connector = new FakeConnector(Resolver.Resolve("Nantes").Network);
        var clock = new Clock(DateTimeOffset.UtcNow);
        var service = new AccountSynchronizationService(options, new LibraryConnectorResolver([connector]), Database, clock,
            NullLogger<AccountSynchronizationService>.Instance);
        await service.SynchronizeAllAsync(CancellationToken.None);
        connector.FailedAccount = "second-account";
        connector.Title = "Updated";
        clock.Now = clock.Now.AddHours(1);
        await service.SynchronizeAllAsync(CancellationToken.None);
        var state = Assert.Single(await Database.GetReservationNetworkStatesAsync(CancellationToken.None));
        Assert.Equal(SynchronizationResult.Partial, state.Result);
        Assert.Equal(clock.Now.AddHours(-1), state.LastCompleteSuccessAt);
        Assert.Contains(await Database.GetReservationsAsync(CancellationToken.None), r => r.Item.Title == "Initial");
        var never = WebUtility.HtmlDecode(await client.GetStringAsync("/Reservations", TestContext.Current.CancellationToken));
        Assert.Contains("n’ont encore jamais été synchronisées complètement", never);
        var filtered = WebUtility.HtmlDecode(await client.GetStringAsync("/Reservations?SelectedStatus=Suspended", TestContext.Current.CancellationToken));
        Assert.Contains("Aucune réservation ne correspond à ces filtres", filtered);
        foreach (var network in new[] { "Nantes", "Nozay" })
        {
            var accountId = network == "Nantes" ? "nantes-account" : "nozay-account";
            await SeedAsync(accountId, network);
            var descriptor = Resolver.Resolve(network).Network;
            await Database.SetReservationNetworkStateAsync(descriptor.Key, descriptor.DisplayName,
                DateTimeOffset.UtcNow, SynchronizationResult.Success, [accountId], CancellationToken.None);
        }
        var empty = WebUtility.HtmlDecode(await client.GetStringAsync("/Reservations", TestContext.Current.CancellationToken));
        Assert.Contains("Aucune réservation en cours", empty);
        Assert.DoesNotContain("ne sont pas encore disponibles", empty);
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task MobilePageFitsViewportAndShowsPickupAndSuspensionDetails()
    {
        var client = Factory.CreateClient();
        await SeedAsync("nantes-account", "Nantes", Item("1", "Document disponible") with
        {
            PickupDeadline = DateOnly.FromDateTime(DateTime.Today),
            StatusLabel = "Disponible"
        },
            Item("2", "Document suspendu", ReservationStatus.Suspended) with
            {
                SuspensionStartsOn = Observation,
                SuspensionEndsOn = Observation.AddDays(10),
                PickupDeadline = null
            });
        var html = await client.GetStringAsync("/Reservations", TestContext.Current.CancellationToken);
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 375, Height = 812 } });
        await page.SetContentAsync(html);
        var environment = Factory.Services.GetRequiredService<IWebHostEnvironment>();
        await page.AddStyleTagAsync(new()
        {
            Content = await File.ReadAllTextAsync(
            Path.Combine(environment.ContentRootPath, "wwwroot", "css", "site.css"), TestContext.Current.CancellationToken)
        });
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= window.innerWidth"));
        Assert.Equal(4, await page.Locator("select").CountAsync());
        Assert.Contains("Retrait au plus tard le", await page.InnerTextAsync("body"));
        Assert.Contains("Fin de suspension le", await page.TextContentAsync("body"));
        await page.SetViewportSizeAsync(1280, 900);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= window.innerWidth"));
    }

    [Fact]
    public async Task VersionOneMigrationKeepsLoansAndInvalidRefreshIsAtomic()
    {
        var database = Database;
        var account = Account("nantes-account", "Nantes", "Lecteur Alpha");
        var network = Resolver.Resolve("Nantes").Network;
        await database.ReplaceAccountLoansAsync(account, network,
            [new LoanSnapshot("1", "Lecteur Alpha", "Prêt conservé", null, null, null, null, Observation)],
            DateTimeOffset.UtcNow, CancellationToken.None);
        var options = Factory.Services.GetRequiredService<IOptions<LibragoOptions>>();
        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={options.Value.DatabasePath};Pooling=False"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE reservations; DROP TABLE reservation_network_sync; PRAGMA user_version = 1;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        await database.InitializeAsync(CancellationToken.None);
        Assert.Equal("Prêt conservé", Assert.Single(await database.GetLoansAsync(CancellationToken.None)).Title);
        await SeedAsync("nantes-account", "Nantes", Item("1", "Original"));
        await Assert.ThrowsAsync<LibraryConnectorException>(() => database.ReplaceAccountReservationsAsync(
            account, network, [Item("1", "Duplicate"), Item("1", "Duplicate")], Observation.AddDays(1), CancellationToken.None));
        Assert.Equal("Original", Assert.Single(await database.GetReservationsAsync(CancellationToken.None)).Item.Title);
    }

    private sealed class FakeConnector(LibraryNetworkDescriptor network) : ILibraryConnector
    {
        public LibraryNetworkDescriptor Network => network;
        public string Title { get; set; } = "Initial";
        public bool FailReservations { get; set; }
        public bool FailLoans { get; set; }
        public string? FailedAccount { get; set; }
        public Task<AccountSnapshot> GetAccountSnapshotAsync(LibraryAccountOptions account, CancellationToken cancellationToken)
        {
            var failReservations = FailReservations || account.AccountId == FailedAccount;
            var failLoans = FailLoans;
            return Task.FromResult(new AccountSnapshot(
                ConnectorResult<IReadOnlyList<LoanSnapshot>>.Create(failLoans ? default : [],
                    failLoans ? LibraryConnectorFailureKind.Upstream : null),
                ConnectorResult<IReadOnlyList<ReservationSnapshot>>.Create(failReservations ? default : [Item("1", Title)],
                    failReservations ? LibraryConnectorFailureKind.Upstream : null)));
        }
    }
    private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), "librago-tests", Guid.NewGuid().ToString("N"));
    private WebApplicationFactory<Program>? _factory;
    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_temporaryDirectory);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(TestConfiguration());
            });
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                var worker = services.Single(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType == typeof(SynchronizationWorker));
                services.Remove(worker);
            });
        });

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        Directory.Delete(_temporaryDirectory, recursive: true);
    }

    private WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("The test application is not initialized.");

    private Dictionary<string, string?> TestConfiguration() => new()
    {
        ["Librago:DatabasePath"] = Path.Combine(_temporaryDirectory, "integration.db"),
        ["Librago:SynchronizationInterval"] = "06:00:00",
        ["Librago:TimeZone"] = "Europe/Paris",
        ["Librago:Accounts:0:AccountId"] = "nantes-account",
        ["Librago:Accounts:0:Network"] = "Nantes",
        ["Librago:Accounts:0:Borrower"] = "Lecteur Alpha",
        ["Librago:Accounts:0:Username"] = "synthetic-user",
        ["Librago:Accounts:0:Password"] = "synthetic-password",
        ["Librago:Accounts:1:AccountId"] = "nozay-account",
        ["Librago:Accounts:1:Network"] = "Nozay",
        ["Librago:Accounts:1:Borrower"] = "",
        ["Librago:Accounts:1:Username"] = "synthetic-user",
        ["Librago:Accounts:1:Password"] = "synthetic-password"
    };

    private static LibraryAccountOptions Account(string id, string network, string borrower) =>
        new()
        {
            AccountId = id,
            Network = network,
            Borrower = borrower,
            Username = "synthetic-user",
            Password = "synthetic-password"
        };

}
