using Librago.Configuration;
using Microsoft.Extensions.Options;

namespace Librago.Synchronization;

public sealed partial class SynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<LibragoOptions> options,
    ILogger<SynchronizationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunSynchronizationAsync(stoppingToken);

        using var timer = new PeriodicTimer(options.Value.SynchronizationInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSynchronizationAsync(stoppingToken);
        }
    }

    private async Task RunSynchronizationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<LoanSynchronizationService>();
            await synchronizer.SynchronizeAllAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogCycleFailure(logger, exception);
        }
    }

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "The synchronization cycle failed unexpectedly.")]
    private static partial void LogCycleFailure(ILogger logger, Exception exception);
}
