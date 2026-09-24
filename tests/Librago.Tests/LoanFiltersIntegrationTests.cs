using System.Text.RegularExpressions;
using Librago.Configuration;
using Librago.Connectors;
using Librago.Loans;
using Librago.Persistence;
using Librago.Synchronization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Librago.Tests;

public sealed class LoanFiltersIntegrationTests : IAsyncLifetime
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "librago-tests",
        Guid.NewGuid().ToString("N"));
    private WebApplicationFactory<Program>? _factory;

    [Fact]
    public async Task GetFiltersLoansAndKeepsSelections()
    {
        var client = Factory.CreateClient();
        await SeedLoansAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        var unconfigured = await client.GetStringAsync("/", cancellationToken);
        Assert.DoesNotContain("Removed account loan", unconfigured);

        var byNetwork = await client.GetStringAsync("/?SelectedNetwork=nozay", cancellationToken);
        Assert.Contains("Nozay Alpha", byNetwork);
        Assert.Contains("Nozay Beta", byNetwork);
        Assert.DoesNotContain("Nantes Alpha", byNetwork);
        Assert.Matches(SelectedOption("nozay"), byNetwork);

        var byBorrower = await client.GetStringAsync("/?SelectedBorrower=Lecteur%20Alpha", cancellationToken);
        Assert.Contains("Nantes Alpha", byBorrower);
        Assert.Contains("Nozay Alpha", byBorrower);
        Assert.DoesNotContain("Nozay Beta", byBorrower);
        Assert.Matches(SelectedOption("Lecteur Alpha"), byBorrower);

        var combined = await client.GetStringAsync(
            "/?SelectedNetwork=nozay&SelectedBorrower=Lecteur%20Alpha",
            cancellationToken);
        Assert.Contains("Nozay Alpha", combined);
        Assert.DoesNotContain("Nantes Alpha", combined);
        Assert.DoesNotContain("Nozay Beta", combined);
    }

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

    private async Task SeedLoansAsync()
    {
        var database = Factory.Services.GetRequiredService<LibragoDatabase>();
        var resolver = Factory.Services.GetRequiredService<LibraryConnectorResolver>();
        var refreshedAt = new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

        await SeedAccountAsync(
            database,
            resolver,
            Account("nantes-account", "Nantes", "Lecteur Alpha"),
            [Snapshot("nantes-alpha", "Lecteur Alpha", "Nantes Alpha")],
            refreshedAt);
        await SeedAccountAsync(
            database,
            resolver,
            Account("removed-account", "Nozay", ""),
            [Snapshot("removed", "Lecteur supprimé", "Removed account loan")],
            refreshedAt);
        await SeedAccountAsync(
            database,
            resolver,
            Account("nozay-account", "Nozay", ""),
            [
                Snapshot("nozay-alpha", "Lecteur Alpha", "Nozay Alpha"),
                Snapshot("nozay-beta", "Lecteur Beta", "Nozay Beta")
            ],
            refreshedAt);
    }

    private static async Task SeedAccountAsync(
        LibragoDatabase database,
        LibraryConnectorResolver resolver,
        LibraryAccountOptions account,
        IReadOnlyCollection<LoanSnapshot> loans,
        DateTimeOffset refreshedAt)
    {
        var network = resolver.Resolve(account.Network).Network;
        await database.ReplaceAccountLoansAsync(
            account,
            network,
            loans,
            refreshedAt,
            CancellationToken.None);
        await database.SetNetworkStateAsync(
            network.Key,
            network.DisplayName,
            refreshedAt,
            SynchronizationResult.Success,
            [account.AccountId],
            CancellationToken.None);
    }

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

    private static LoanSnapshot Snapshot(string id, string borrower, string title) =>
        new(
            id,
            borrower,
            title,
            "Auteur synthétique",
            "Livre",
            "Bibliothèque exemple",
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 20));

    private static Regex SelectedOption(string value) =>
        new($"<option(?=[^>]*value=\"{Regex.Escape(value)}\")(?=[^>]*selected=\"selected\")[^>]*>");
}
