using System.ComponentModel.DataAnnotations;

namespace VoiceApi.Infrastructure.Options;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public double AccessTokenExpirationMinutes { get; set; }

    [Range(1, 365)]
    public double RefreshTokenExpirationDays { get; set; }
}
