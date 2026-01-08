using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoiceApi.Domain;

public class Utterance : BaseEntity
{
    // Id and CreatedAt are inherited

    [Required]
    public Guid SectionId { get; set; }

    public string? OriginalText { get; set; }
    public string? TranslatedText { get; set; }
    public string? OriginalAudioPath { get; set; }
    public string? TranslatedAudioPath { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(SectionId))]
    public Section Section { get; set; } = null!;
}
