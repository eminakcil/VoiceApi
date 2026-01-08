using System.ComponentModel.DataAnnotations;

namespace VoiceApi.Domain;

public abstract class BaseEntity : ISoftDelete
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }

    public void Undo()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}
