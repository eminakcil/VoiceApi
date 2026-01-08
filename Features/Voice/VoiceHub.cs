using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using VoiceApi.Infrastructure.Options;

namespace VoiceApi.Features.Voice;

[Authorize]
public class VoiceHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly ConcurrentDictionary<string, AzureTranscriptionManager> _managers =
        new();
    private readonly AzureSettings _azureSettings;
    private readonly IHubContext<VoiceHub> _hubContext;

    public VoiceHub(
        IOptions<AzureSettings> azureSettings,
        IHubContext<VoiceHub> hubContext,
        IServiceScopeFactory scopeFactory
    )
    {
        _azureSettings = azureSettings.Value;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task StartSection(StartSectionRequest request)
    {
        var userIdString = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userIdString))
            throw new HubException("Yetkisiz erişim!");

        var userId = Guid.Parse(userIdString);

        using var scope = _scopeFactory.CreateScope();
        var voiceService = scope.ServiceProvider.GetRequiredService<IVoiceService>();

        // DB'de section oluştur
        var section = await voiceService.CreateSectionAsync(request, userId);

        // Manager oluştur ve başlat
        var manager = new AzureTranscriptionManager(
            section.Id,
            Context.ConnectionId,
            request.SourceLanguage,
            request.TargetLanguage,
            _azureSettings.Key,
            _azureSettings.Region,
            request.IsMuted,
            _hubContext,
            _scopeFactory
        );

        await manager.StartAsync();
        _managers[Context.ConnectionId] = manager;
    }

    public async Task SendAudio(byte[] chunk)
    {
        if (_managers.TryGetValue(Context.ConnectionId, out var manager))
        {
            manager.ProvideAudio(chunk);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_managers.TryRemove(Context.ConnectionId, out var manager))
        {
            await manager.StopAsync();

            var originalPath = manager.GetOriginalPath();
            var translatedPath = ""; // manager.GetTranslatedPath();
            var sectionId = manager.GetSectionId();

            using var scope = _scopeFactory.CreateScope();
            var voiceService = scope.ServiceProvider.GetRequiredService<IVoiceService>();
            await voiceService.EndSectionAsync(sectionId, originalPath, translatedPath);

            manager.Dispose();
        }
        await base.OnDisconnectedAsync(exception);
    }
}
