using Microsoft.Extensions.Configuration;

namespace CoreFutsal.Shared.Extensions;

public static class ConfigurationExtensions
{
    private static readonly string[] RequiredKeys =
    [
        "Jwt:Key",
        "Jwt:Issuer",
        "Jwt:Audience",
        "ConnectionStrings:futsalConn"
    ];

    public static void ValidateRequiredSecrets(this IConfiguration config)
    {
        var missing = RequiredKeys
            .Where(key => string.IsNullOrWhiteSpace(config[key]))
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Required configuration values are missing or empty: {string.Join(", ", missing)}. " +
                "Set them via environment variables, User Secrets (dev), or your secrets manager (prod).");
    }
}
