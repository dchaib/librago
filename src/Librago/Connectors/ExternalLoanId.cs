using System.Security.Cryptography;
using System.Text;

namespace Librago.Connectors;

internal static class ExternalLoanId
{
    public static string FromFallback(params string?[] parts)
    {
        var value = string.Join('\u001f', parts.Select(part => part?.Trim() ?? string.Empty));
        return $"generated:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))}";
    }
}

