using VoiceApi.Domain;
using VoiceApi.Shared.Primitives;

namespace VoiceApi.Features.Voice;

public interface IVoiceService
{
    Task<Section> CreateSectionAsync(StartSectionRequest request);
    Task SaveUtteranceAsync(Guid sectionId, string original, string translated);
    Task<Result<bool>> EndSectionAsync(Guid sectionId);
}
