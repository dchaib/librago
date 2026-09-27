using Librago.Connectors;
using Librago.Connectors.Nantes;
using Microsoft.Extensions.Logging;

namespace Librago.Tests;

public sealed class NantesDiagnosticsTests
{
    [Theory]
    [InlineData("The Nantes loans response was not valid JSON in the expected format.", "InvalidLoansJson")]
    [InlineData("The Nantes loans response was incomplete.", "InvalidLoansEnvelope")]
    [InlineData("The Nantes loans response did not contain its items.", "MissingLoanItems")]
    [InlineData("The Nantes loans response contained duplicate identities.", "DuplicateLoanIdentities")]
    [InlineData("The Nantes loans response ended before all reported loans were returned.", "LoanCountMismatch")]
    [InlineData("sensitive synthetic response", "UnexpectedResponse")]
    public async Task LogsOnlyFixedDiagnosticCodesAndPreservesFailure(string message, string reason)
    {
        var logger = new RecordingLogger();
        var connector = new NantesLibraryConnector(logger);
        Func<Task<string>> action = () => throw new LibraryConnectorException(LibraryConnectorFailureKind.UnexpectedResponse, message);
        var result = await Capture(connector, action);
        Assert.False(result.IsSuccess);
        Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, result.FailureKind);
        Assert.Equal($"Nantes loans retrieval failed ({reason}; exception type LibraryConnectorException).", Assert.Single(logger.Messages));
    }

    [Fact]
    public async Task UnexpectedExceptionLogsTypeWithoutMessageOrStackTrace()
    {
        var logger = new RecordingLogger();
        var connector = new NantesLibraryConnector(logger);
        var result = await Capture(connector, () => throw new InvalidOperationException("sensitive synthetic response"));
        Assert.Equal(LibraryConnectorFailureKind.UnexpectedResponse, result.FailureKind);
        Assert.Equal("Nantes loans retrieval failed (UnhandledException; exception type InvalidOperationException).", Assert.Single(logger.Messages));
    }

    [Fact]
    public async Task CallerCancellationStillPropagatesWithoutDiagnosticWarning()
    {
        var logger = new RecordingLogger();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        var connector = new NantesLibraryConnector(logger);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Capture(connector,
            () => Task.FromCanceled<string>(cancellation.Token), cancellation.Token));
        Assert.Empty(logger.Messages);
    }

    private static Task<ConnectorResult<string>> Capture(NantesLibraryConnector connector, Func<Task<string>> action,
        CancellationToken? cancellationToken = null)
    {
        var method = typeof(NantesLibraryConnector).GetMethod("CaptureAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.MakeGenericMethod(typeof(string));
        return (Task<ConnectorResult<string>>)method.Invoke(connector, [action, "loans", cancellationToken ?? TestContext.Current.CancellationToken])!;
    }

    private sealed class RecordingLogger : ILogger<NantesLibraryConnector>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Equal(LogLevel.Warning, logLevel);
            Assert.Equal(1203, eventId.Id);
            Assert.Null(exception);
            Messages.Add(formatter(state, exception));
        }
    }
}
