using System.Globalization;
using System.Text.Json;
using Librago.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Librago.Persistence;

public sealed partial class LibragoDatabase
{
    private const int CurrentSchemaVersion = 3;
    private readonly string _connectionString;

    public LibragoDatabase(IOptions<LibragoOptions> options, IWebHostEnvironment environment)
    {
        var configuredPath = options.Value.DatabasePath;
        var databasePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }

        var version = await ReadSchemaVersionAsync(connection, cancellationToken);
        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"The database schema version {version} is newer than this application supports.");
        }

        if (version < 1)
        {
            await ApplyVersionOneAsync(connection, cancellationToken);
        }
        if (version < 2)
        {
            await ApplyVersionTwoAsync(connection, cancellationToken);
        }
        if (version < 3)
        {
            await ApplyVersionThreeAsync(connection, cancellationToken);
        }
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            return Convert.ToInt32(
                await command.ExecuteScalarAsync(cancellationToken),
                CultureInfo.InvariantCulture) == 1;
        }
        catch (SqliteException)
        {
            return false;
        }
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateOnly? ReadNullableDate(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : DateOnly.ParseExact(reader.GetString(ordinal), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string SerializeAccountIds(IEnumerable<string> accountIds) =>
        JsonSerializer.Serialize(accountIds
            .OrderBy(accountId => accountId, StringComparer.OrdinalIgnoreCase)
            .ToArray());

    private static string[] DeserializeAccountIds(string value) =>
        JsonSerializer.Deserialize<string[]>(value) ??
        throw new InvalidOperationException("A network synchronization state has invalid account ids.");
}
