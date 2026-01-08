using Microsoft.EntityFrameworkCore;
using VoiceApi.Domain;
using VoiceApi.Infrastructure;
using VoiceApi.Shared.Primitives;

namespace VoiceApi.Features.Voice;

public class VoiceService : IVoiceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<VoiceService> _logger;

    public VoiceService(AppDbContext context, ILogger<VoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Section> CreateSectionAsync(StartSectionRequest request)
    {
        Guid finalSessionId;

        if (request.SessionId == null || request.SessionId == Guid.Empty)
        {
            var newSession = new Session
            {
                UserId = Guid.Parse("019B9E82-BBCB-727D-8B0E-450567D13CB1"),
                Title = $"Oturum - {DateTime.Now:dd.MM.yyyy HH:mm}",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Sessions.Add(newSession);
            await _context.SaveChangesAsync();
            finalSessionId = newSession.Id;
        }
        else
        {
            finalSessionId = request.SessionId.Value;
        }

        _logger.LogInformation("SessionId: {SessionId}", finalSessionId);

        var section = new Section
        {
            SessionId = finalSessionId,
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage,
            StartedAt = DateTime.UtcNow,
            IsMuted = request.IsMuted,
            OriginalAudioPath = null,
            TranslatedAudioPath = null,
        };

        _context.Sections.Add(section);
        await _context.SaveChangesAsync();
        return section;
    }

    public async Task SaveUtteranceAsync(Guid sectionId, string original, string translated)
    {
        var utterance = new Utterance
        {
            SectionId = sectionId,
            OriginalText = original,
            TranslatedText = translated,
        };

        _context.Utterances.Add(utterance);
        await _context.SaveChangesAsync();
    }

    public async Task<Result<bool>> EndSectionAsync(
        Guid sectionId,
        string? originalPath,
        string? translatedPath
    )
    {
        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null)
            return Result.Failure<bool>(new Error("Voice.NotFound", "Section not found"));

        section.EndedAt = DateTime.UtcNow;
        section.OriginalAudioPath = originalPath;
        section.TranslatedAudioPath = translatedPath;

        await _context.SaveChangesAsync();
        return true;
    }
}
