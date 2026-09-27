using Librago.Connectors;
using Librago.Connectors.Nozay;
using Microsoft.Extensions.Logging;

namespace Librago.Tests;

public sealed class NozayBrowserDiagnosticsTests
{
    [Fact]
    public void MissingBrowserExplainsInstallationWithoutLoggingLocalPaths()
    {
        var logger = new CapturingLogger();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "SYNTHETIC_PRIVATE_PATH", "chrome.exe");
        var exception = Assert.Throws<LibraryConnectorException>(() =>
            NozayLibraryConnector.RequireChromiumExecutable(path, logger));
        Assert.Equal(LibraryConnectorFailureKind.Upstream, exception.FailureKind);
        var message = Assert.Single(logger.Messages);
        Assert.Contains("playwright.ps1 install chromium", message);
        Assert.Contains("without --only-shell", message);
        Assert.DoesNotContain("SYNTHETIC_PRIVATE_PATH", message);
    }

    [Fact]
    public void InstalledBrowserPassesWithoutWarning()
    {
        var logger = new CapturingLogger();
        NozayLibraryConnector.RequireChromiumExecutable(typeof(NozayLibraryConnector).Assembly.Location, logger);
        Assert.Empty(logger.Messages);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            Messages.Add(formatter(state, exception));
        }
    }
}
