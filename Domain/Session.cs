using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceApi.Domain;

public class Session : BaseEntity
{
    // Id and CreatedAt are inherited

    [Required]
    public Guid UserId { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Summary { get; set; }

    public DateTime? EndedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
