using Librago.Configuration;
using Librago.Connectors;
using Librago.Persistence;
using Librago.Loans;
using Librago.Synchronization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Librago.Tests;

public sealed class LoanSynchronizationServiceTests
{
    [Fact]
    public async Task PartialRefreshPreservesTheFailedAccountsLastKnownLoans()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "librago-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var accounts = new[]
            {
                Account("account-a"),
                Account("account-b")
            };
            var options = Options.Create(new LibragoOptions
            {
                DatabasePath = Path.Combine(temporaryDirectory, "test.db"),
                Accounts = [.. accounts]
            });
            var environment = new TestWebHostEnvironment(temporaryDirectory);
            var database = new LibragoDatabase(options, environment);
            await database.InitializeAsync(CancellationToken.None);

            var clock = new MutableTimeProvider(
                new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero));
            var connector = new FakeConnector(account => [Snapshot(account.AccountId, "Initial")]);
            var service = CreateService(options, connector, database, clock);

            await service.SynchronizeAllAsync(CancellationToken.None);

            clock.UtcNow = clock.UtcNow.AddHours(1);
            connector.Handler = account => account.AccountId == "account-b"
                ? throw new LibraryConnectorException(
                    LibraryConnectorFailureKind.InvalidData,
                    "Synthetic failure.")
                : [Snapshot(account.AccountId, "Updated")];

            await service.SynchronizeAllAsync(CancellationToken.None);

            var loans = await database.GetLoansAsync(CancellationToken.None);
            var state = Assert.Single(await database.GetNetworkStatesAsync(CancellationToken.None));
            Assert.Contains(loans, loan => loan.AccountId == "account-a" && loan.Title == "Updated");
            Assert.Contains(loans, loan => loan.AccountId == "account-b" && loan.Title == "Initial");
            Assert.Equal(SynchronizationResult.Partial, state.Result);
            Assert.Equal(
                new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero),
                state.LastCompleteSuccessAt);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RemovingAnAccountRemovesItsStoredLoansAndSynchronizationState()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "librago-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var currentAccount = Account("current-account");
            var removedAccount = Account("removed-account");
            var options = Options.Create(new LibragoOptions
            {
                DatabasePath = Path.Combine(temporaryDirectory, "test.db")
            });
            var database = new LibragoDatabase(options, new TestWebHostEnvironment(temporaryDirectory));
            var network = new LibraryNetworkDescriptor("Synthetic", "synthetic-network", "Synthetic network");
            var refreshedAt = new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);
            await database.InitializeAsync(CancellationToken.None);
            await database.ReplaceAccountLoansAsync(
                currentAccount,
                network,
                [Snapshot("current", "Current")],
                refreshedAt,
                CancellationToken.None);
            await database.ReplaceAccountLoansAsync(
                removedAccount,
                network,
                [Snapshot("removed", "Removed")],
                refreshedAt,
                CancellationToken.None);

            await database.RemoveUnconfiguredAccountsAsync(
                [currentAccount.AccountId],
                CancellationToken.None);

            var loans = await database.GetLoansAsync(CancellationToken.None);
            Assert.Single(loans);
            Assert.Equal(currentAccount.AccountId, loans[0].AccountId);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DoesNotLogConnectorExceptionDetails()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "librago-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var account = Account("account-a");
            var options = Options.Create(new LibragoOptions
            {
                DatabasePath = Path.Combine(temporaryDirectory, "test.db"),
                Accounts = [account]
            });
            var database = new LibragoDatabase(options, new TestWebHostEnvironment(temporaryDirectory));
            await database.InitializeAsync(CancellationToken.None);
            var logger = new CapturingLogger<LoanSynchronizationService>();
            var service = new LoanSynchronizationService(
                options,
                new LibraryConnectorResolver([
                    new FakeConnector(_ => throw new LibraryConnectorException(
                        LibraryConnectorFailureKind.Authentication,
                        "SYNTHETIC_AUDIT_PASSWORD"))
                ]),
                database,
                TimeProvider.System,
                logger);

            await service.SynchronizeAllAsync(CancellationToken.None);

            var message = Assert.Single(logger.Messages);
            Assert.DoesNotContain("SYNTHETIC_AUDIT_PASSWORD", message);
            Assert.Contains("Authentication", message);
            Assert.Contains(account.AccountId, message);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static LoanSynchronizationService CreateService(
        IOptions<LibragoOptions> options,
        ILibraryConnector connector,
        LibragoDatabase database,
        TimeProvider timeProvider) =>
        new(
            options,
            new LibraryConnectorResolver([connector]),
            database,
            timeProvider,
            NullLogger<LoanSynchronizationService>.Instance);

    private static LibraryAccountOptions Account(string id) =>
        new()
        {
            AccountId = id,
            Network = "Synthetic",
            Borrower = id,
            Username = "synthetic-user",
            Password = "synthetic-password"
        };

    private static LoanSnapshot Snapshot(string borrower, string title) =>
        new(
            $"loan-{borrower}",
            borrower,
            title,
            "Auteur synthétique",
            "Livre",
            "Bibliothèque exemple",
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 20));

    private sealed class FakeConnector(
        Func<LibraryAccountOptions, IReadOnlyList<LoanSnapshot>> handler) : ILibraryConnector
    {
        public LibraryNetworkDescriptor Network { get; } = new(
            "Synthetic",
            "synthetic-network",
            "Réseau synthétique");

        public Func<LibraryAccountOptions, IReadOnlyList<LoanSnapshot>> Handler { get; set; } = handler;

        public Task<IReadOnlyList<LoanSnapshot>> GetLoansAsync(
            LibraryAccountOptions account,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Handler(account));
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class TestWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Librago.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = contentRootPath;

        public string EnvironmentName { get; set; } = "Testing";

        public string ContentRootPath { get; set; } = contentRootPath;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Assert.Null(exception);
        }
    }
}
