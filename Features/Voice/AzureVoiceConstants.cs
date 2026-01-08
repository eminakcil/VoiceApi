namespace VoiceApi.Features.Voice;

public static class AzureVoiceConstants
{
    private static readonly Dictionary<string, string> LanguageToVoiceMap = new()
    {
        { "tr-TR", "tr-TR-EmelNeural" },
        { "en-US", "en-US-JennyNeural" },
        { "en-GB", "en-GB-SoniaNeural" },
        { "de-DE", "de-DE-KatjaNeural" },
        { "fr-FR", "fr-FR-DeniseNeural" },
        { "es-ES", "es-ES-ElviraNeural" },
        { "ru-RU", "ru-RU-SvetlanaNeural" },
        { "it-IT", "it-IT-ElsaNeural" },
    };

    public static string GetVoiceName(string languageCode)
    {
        return LanguageToVoiceMap.GetValueOrDefault(languageCode, "en-US-JennyNeural");
    }
}
