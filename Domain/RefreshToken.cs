using System.ComponentModel.DataAnnotations;

namespace VoiceApi.Domain;

public class RefreshToken : BaseEntity
{
    // Id, CreatedAt (was Created) are inherited

    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime Expires { get; set; }

    // public DateTime Created { get; set; } -> Replaced by inherited CreatedAt

    public DateTime? Revoked { get; set; }

    public bool IsUsed { get; set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;

    public bool IsRevoked => Revoked != null;

    public bool IsActive => !IsRevoked && !IsExpired;

    public Guid UserId { get; set; } // Changed to Guid

    public User? User { get; set; }
}
