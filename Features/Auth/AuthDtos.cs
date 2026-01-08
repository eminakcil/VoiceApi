using System.ComponentModel.DataAnnotations;

namespace VoiceApi.Features.Auth;

// DTOs
public record RegisterRequest(string Username, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string Token, string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime Expiration);

public record RevokeTokenRequest(string RefreshToken);
