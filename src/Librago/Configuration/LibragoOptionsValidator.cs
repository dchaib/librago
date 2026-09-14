using Microsoft.Extensions.Options;

namespace Librago.Configuration;

public sealed class LibragoOptionsValidator : IValidateOptions<LibragoOptions>
{
    private static readonly HashSet<string> SupportedNetworks =
        new(StringComparer.OrdinalIgnoreCase) { "Nantes", "Nozay" };

    public ValidateOptionsResult Validate(string? name, LibragoOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DatabasePath))
        {
            return ValidateOptionsResult.Fail("Librago:DatabasePath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DataProtectionPath))
        {
            return ValidateOptionsResult.Fail("Librago:DataProtectionPath is required.");
        }

        if (options.SynchronizationInterval < TimeSpan.FromMinutes(5))
        {
            return ValidateOptionsResult.Fail(
                "Librago:SynchronizationInterval must be at least five minutes.");
        }

        if (string.IsNullOrWhiteSpace(options.TimeZone))
        {
            return ValidateOptionsResult.Fail("Librago:TimeZone is required.");
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
        }
        catch (Exception exception) when (
            exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return ValidateOptionsResult.Fail(
                $"Librago:TimeZone '{options.TimeZone}' is not available on this system.");
        }

        foreach (var account in options.Accounts)
        {
            if (string.IsNullOrWhiteSpace(account.AccountId) ||
                string.IsNullOrWhiteSpace(account.Network) ||
                string.IsNullOrWhiteSpace(account.Username) ||
                string.IsNullOrWhiteSpace(account.Password))
            {
                return ValidateOptionsResult.Fail(
                    "Every configured library account requires an account id, network, username, and password.");
            }

            if (!SupportedNetworks.Contains(account.Network))
            {
                return ValidateOptionsResult.Fail(
                    $"Library account '{account.AccountId}' uses unsupported network '{account.Network}'.");
            }

            if (account.Network.Equals("Nantes", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(account.Borrower))
            {
                return ValidateOptionsResult.Fail(
                    $"Nantes account '{account.AccountId}' requires a borrower display name.");
            }
        }

        var duplicateId = options.Accounts
            .GroupBy(account => account.AccountId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
        {
            return ValidateOptionsResult.Fail(
                $"Library account id '{duplicateId.Key}' is configured more than once.");
        }

        return ValidateOptionsResult.Success;
    }
}
