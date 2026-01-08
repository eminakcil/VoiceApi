namespace VoiceApi.Infrastructure.Options;

public class AzureSettings
{
    public const string SectionName = "AzureSettings";
    public string Key { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}
