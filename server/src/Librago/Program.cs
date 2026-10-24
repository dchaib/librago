using Librago.Configuration;
using Librago.Connectors;
using Librago.Connectors.Nantes;
using Librago.Connectors.Nozay;
using Librago.Persistence;
using Librago.Loans;
using Librago.Synchronization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss zzz ";
});

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: false);

const string externalConfigurationDirectory = "/config";
if (Directory.Exists(externalConfigurationDirectory))
{
    builder.Configuration.AddJsonFile(
        new PhysicalFileProvider(externalConfigurationDirectory),
        "appsettings.Local.json",
        optional: true,
        reloadOnChange: false);
}

builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddOptions<LibragoOptions>()
    .Bind(builder.Configuration.GetSection(LibragoOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<LibragoOptions>, LibragoOptionsValidator>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LibragoDatabase>();
builder.Services.AddSingleton<ILibraryConnector, NantesLibraryConnector>();
builder.Services.AddSingleton<ILibraryConnector, NozayLibraryConnector>();
builder.Services.AddSingleton<LibraryConnectorResolver>();
builder.Services.AddScoped<AccountSynchronizationService>();
builder.Services.AddHostedService<SynchronizationWorker>();
builder.Services.AddRazorPages();
builder.Services.AddScoped<LoanQuery>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// API routes are a local migration slice until authentication is implemented.
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next(context);
        return;
    }

    context.Response.Headers.CacheControl = "no-store";
    try
    {
        await next(context);
        if (context.Response.StatusCode >= 400 && !context.Response.HasStarted &&
            context.Response.ContentType is null)
            await Results.Problem(statusCode: context.Response.StatusCode).ExecuteAsync(context);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        context.Abort();
    }
    catch (Exception exception) when (!context.Response.HasStarted)
    {
        // Exception messages and request URLs can contain private application data.
        LogApiReadFailure(app.Logger, exception.GetType().Name);
        context.Response.Clear();
        context.Response.Headers.CacheControl = "no-store";
        await Results.Problem(statusCode: 500, title: "Unable to read application data.").ExecuteAsync(context);
    }
});

var database = app.Services.GetRequiredService<LibragoDatabase>();
await database.InitializeAsync(CancellationToken.None);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapGet("/health", async (LibragoDatabase database, CancellationToken cancellationToken) =>
    await database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapRazorPages();
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/loans", (LoanQuery query, CancellationToken cancellationToken) =>
        query.ExecuteAsync(cancellationToken));
}

await app.RunAsync();

public partial class Program
{
    [LoggerMessage(EventId = 1003, Level = LogLevel.Error,
        Message = "API data read failed ({FailureType}).")]
    private static partial void LogApiReadFailure(ILogger logger, string failureType);
}
