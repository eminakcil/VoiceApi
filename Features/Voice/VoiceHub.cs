using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using VoiceApi.Infrastructure.Options;

namespace VoiceApi.Features.Voice;

public class VoiceHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly ConcurrentDictionary<string, AzureTranscriptionManager> _managers =
        new();
    private readonly IServiceProvider _serviceProvider;
    private readonly AzureSettings _azureSettings;
    private readonly IHubContext<VoiceHub> _hubContext;

    public VoiceHub(
        IServiceProvider serviceProvider,
        IOptions<AzureSettings> azureSettings,
        IHubContext<VoiceHub> hubContext,
        IServiceScopeFactory scopeFactory
    )
    {
        _serviceProvider = serviceProvider;
        _azureSettings = azureSettings.Value;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task StartSection(StartSectionRequest request)
    {
        using var scope = _serviceProvider.CreateScope();
        var voiceService = scope.ServiceProvider.GetRequiredService<IVoiceService>();

        // DB'de section oluştur
        var section = await voiceService.CreateSectionAsync(request);

        // Manager oluştur ve başlat
        var manager = new AzureTranscriptionManager(
            section.Id,
            Context.ConnectionId,
            request.SourceLanguage,
            request.TargetLanguage,
            _azureSettings.Key,
            _azureSettings.Region,
            _serviceProvider,
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
            manager.Dispose();
        }
        await base.OnDisconnectedAsync(exception);
    }
}
