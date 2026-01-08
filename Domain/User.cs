using System.ComponentModel.DataAnnotations;

namespace VoiceApi.Domain;

public class User : BaseEntity
{
    // Id and CreatedAt are inherited

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    // Navigation Property
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
