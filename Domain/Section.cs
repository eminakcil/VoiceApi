using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceApi.Domain;

public class Section : BaseEntity
{
    // Id and CreatedAt are inherited

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MaxLength(10)]
    public string SourceLanguage { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string TargetLanguage { get; set; } = string.Empty;

    public string? OriginalAudioPath { get; set; }
    public string? TranslatedAudioPath { get; set; }
    public bool IsMuted { get; set; } = false;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(SessionId))]
    public Session Session { get; set; } = null!;

    public ICollection<Utterance> Utterances { get; set; } = new List<Utterance>();
}
