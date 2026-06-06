using Microsoft.Extensions.Configuration;

namespace CoreFutsal.Tests.Helpers;

public static class TestConfig
{
    public static IConfiguration Create() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-jwt-key-that-is-at-least-32-characters-long!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpiryMinutes"] = "30",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();
}
