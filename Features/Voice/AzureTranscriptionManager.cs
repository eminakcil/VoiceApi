using Microsoft.AspNetCore.SignalR;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.DependencyInjection;

namespace VoiceApi.Features.Voice;

public class AzureTranscriptionManager : IDisposable
{
    private readonly Guid _sectionId;
    private readonly string _connectionId;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<VoiceHub> _hubContext;
    private readonly TranslationRecognizer _recognizer;
    private readonly PushAudioInputStream _audioInputStream;
    private readonly FileStream _rawStream;
    private readonly string _rawPath;

    private readonly string _translatedPath;
    private readonly FileStream _translatedStream;
    private readonly bool _isMuted;

    private readonly IServiceScopeFactory _scopeFactory;

    public AzureTranscriptionManager(
        Guid sectionId,
        string connectionId,
        string sourceLang,
        string targetLang,
        string key,
        string region,
        bool IsMuted,
        IHubContext<VoiceHub> hubContext,
        IServiceScopeFactory scopeFactory
    )
    {
        _sectionId = sectionId;
        _connectionId = connectionId;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _isMuted = IsMuted;

        _translatedPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "recordings",
            $"{_sectionId}_translated.raw"
        );

        if (!_isMuted)
        {
            _translatedStream = new FileStream(
                _translatedPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite
            );
        }

        // 1. Dosya Sistemi Hazırlığı
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "recordings");
        Directory.CreateDirectory(folderPath);
        _rawPath = Path.Combine(folderPath, $"{_sectionId}.raw");
        _rawStream = new FileStream(
            _rawPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite
        );

        // 2. Azure Config
        var config = SpeechTranslationConfig.FromSubscription(key, region);
        config.SpeechRecognitionLanguage = sourceLang;
        config.AddTargetLanguage(targetLang);

        config.VoiceName = AzureVoiceConstants.GetVoiceName(targetLang);

        _audioInputStream = AudioInputStream.CreatePushStream();
        _recognizer = new TranslationRecognizer(
            config,
            AudioConfig.FromStreamInput(_audioInputStream)
        );

        // 3. Events
        _recognizer.Recognized += async (s, e) => await HandleRecognized(e, targetLang);

        _recognizer.Synthesizing += (s, e) =>
        {
            var audioData = e.Result.GetAudio();
            if (audioData != null && audioData.Length > 0)
            {
                // Sesi hem dosyaya yaz hem de Hub üzerinden gönder
                HandleSynthesizing(audioData);
            }
        };
    }

    private async Task HandleRecognized(TranslationRecognitionEventArgs e, string targetLang)
    {
        if (e.Result.Reason == ResultReason.TranslatedSpeech)
        {
            using var scope = _scopeFactory.CreateScope();
            var voiceService = scope.ServiceProvider.GetRequiredService<IVoiceService>();

            var original = e.Result.Text;
            var translated = e.Result.Translations[targetLang];

            // DB'ye kaydet
            await voiceService.SaveUtteranceAsync(_sectionId, original, translated);

            // UI'a anlık bas
            await _hubContext
                .Clients.Client(_connectionId)
                .SendAsync(
                    "ReceiveUtterance",
                    new
                    {
                        OriginalText = original,
                        TranslatedText = translated,
                        SectionId = _sectionId,
                    }
                );
        }
    }

    private void HandleSynthesizing(byte[] audioData)
    {
        if (_isMuted)
            return;

        _translatedStream.Write(audioData, 0, audioData.Length);

        var base64Audio = Convert.ToBase64String(audioData);
        _hubContext.Clients.Client(_connectionId).SendAsync("ReceiveAudio", base64Audio);
    }

    public void ProvideAudio(byte[] data)
    {
        _audioInputStream.Write(data);
        _rawStream.Write(data, 0, data.Length); // Aynı zamanda dosyaya yaz
    }

    public async Task StartAsync() => await _recognizer.StartContinuousRecognitionAsync();

    public async Task StopAsync()
    {
        await _recognizer.StopContinuousRecognitionAsync();
        await _rawStream.DisposeAsync();
        CreateWavFile(_rawPath); // Kapatırken WAV'a çevir

        if (!_isMuted && _translatedStream != null)
        {
            await _translatedStream.DisposeAsync();
            CreateWavFile(_translatedPath);
        }
    }

    private void CreateWavFile(string rawPath)
    {
        var wavPath = rawPath.Replace(".raw", ".wav");
        var rawBytes = File.ReadAllBytes(rawPath);
        using var fs = new FileStream(wavPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        bw.Write("RIFF".ToCharArray());
        bw.Write(36 + rawBytes.Length);
        bw.Write("WAVE".ToCharArray());
        bw.Write("fmt ".ToCharArray());
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write(16000); // SampleRate
        bw.Write(16000 * 1 * 16 / 8);
        bw.Write((short)(1 * 16 / 8));
        bw.Write((short)16);
        bw.Write("data".ToCharArray());
        bw.Write(rawBytes.Length);
        bw.Write(rawBytes);

        if (File.Exists(rawPath))
            File.Delete(rawPath); // Ham dosyayı temizle
    }

    public void Dispose()
    {
        _recognizer.Dispose();
        _audioInputStream.Dispose();
    }

    public string GetOriginalPath() => _rawPath.Replace(".raw", ".wav");

    public string GetTranslatedPath() => _translatedPath;

    public Guid GetSectionId() => _sectionId;
}
