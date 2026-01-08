using VoiceApi.Shared.Models;
using VoiceApi.Shared.Primitives;

namespace VoiceApi.Features.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<Result<bool>> RevokeTokenAsync(string token);
}
