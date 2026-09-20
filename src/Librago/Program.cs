using Librago.Configuration;
using Librago.Connectors;
using Librago.Connectors.Nantes;
using Librago.Connectors.Nozay;
using Librago.Persistence;
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
builder.Services.AddScoped<LoanSynchronizationService>();
builder.Services.AddHostedService<SynchronizationWorker>();
builder.Services.AddRazorPages();

var app = builder.Build();

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

await app.RunAsync();

public partial class Program;
