namespace VoiceApi.Features.Voice;

public record StartSectionRequest(
    Guid? SessionId,
    string SourceLanguage,
    string TargetLanguage,
    bool IsMuted = false
);

public record AudioPacket(byte[] Data);

public record UtteranceResponse(
    Guid Id,
    string OriginalText,
    string TranslatedText,
    DateTime CreatedAt
);
