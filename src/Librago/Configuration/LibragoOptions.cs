namespace Librago.Configuration;

public sealed class LibragoOptions
{
    public const string SectionName = "Librago";

    public string DatabasePath { get; init; } = "/data/librago.db";

    public TimeSpan SynchronizationInterval { get; init; } = TimeSpan.FromHours(6);

    public string TimeZone { get; init; } = "Europe/Paris";

    public List<LibraryAccountOptions> Accounts { get; init; } = [];
}

public sealed class LibraryAccountOptions
{
    public required string AccountId { get; init; }

    public required string Network { get; init; }

    public string Borrower { get; init; } = string.Empty;

    public Dictionary<string, string> BorrowerAliases { get; init; } = [];

    public required string Username { get; init; }

    public required string Password { get; init; }
}
