using Microsoft.Extensions.Configuration;

namespace ProjectFlow.Api.Configuration;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    [ConfigurationKeyName("AccessTokenExpirationMinutes")]
    public int ExpirationMinutes { get; set; }

    public int RefreshTokenExpirationDays { get; set; } 
}