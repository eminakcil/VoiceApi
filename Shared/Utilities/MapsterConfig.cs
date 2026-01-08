using Mapster;
using VoiceApi.Domain;
using VoiceApi.Features.Auth;

namespace VoiceApi.Shared.Utilities;

public static class MapsterConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<RegisterRequest, User>
            .NewConfig()
            .Map(dest => dest.PasswordHash, src => src.Password)
            .Ignore(dest => dest.Id) // Ignore Id, it's auto-generated
            .Ignore(dest => dest.PasswordHash) // We manual hash
            .Map(dest => dest.Roles, src => new List<string> { "User" });
    }
}
