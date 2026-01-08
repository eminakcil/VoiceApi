using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VoiceApi.Domain;
using VoiceApi.Infrastructure;
using VoiceApi.Shared.Models;
using VoiceApi.Shared.Primitives;

namespace VoiceApi.Features.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly VoiceApi.Infrastructure.Options.JwtSettings _jwtSettings;

    public AuthService(
        AppDbContext context,
        Microsoft.Extensions.Options.IOptions<VoiceApi.Infrastructure.Options.JwtSettings> jwtSettings
    )
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // 1. Check if user exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return Result.Failure<AuthResponse>(
                new Error("Auth.EmailUsed", "Email already registered.")
            );

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return Result.Failure<AuthResponse>(new Error("Auth.UsernameUsed", "Username taken."));

        // 2. Hash Password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Create User (Mapster)
        var user = request.Adapt<User>();
        user.PasswordHash = passwordHash;
        // Roles is mapped via config or we can ensure it here
        if (user.Roles == null || !user.Roles.Any())
            user.Roles = new List<string> { "User" };

        // 4. Generate Tokens (Before saving user)
        var (accessToken, refreshToken) = GenerateTokens(user);
        user.RefreshTokens.Add(refreshToken);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var authResponse = new AuthResponse(accessToken, refreshToken.Token, refreshToken.Expires);

        return authResponse;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Security best practice: generic message
            return Result.Failure<AuthResponse>(
                new Error("Auth.InvalidCredentials", "Invalid credentials.")
            );
        }

        var authResponse = await CreateAndSaveAuthResponseAsync(user);
        return authResponse;
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _context
            .Users.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.RefreshToken));

        if (user == null)
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidToken", "Invalid token."));

        var refreshToken = user.RefreshTokens.Single(x => x.Token == request.RefreshToken);

        // Token Rotation Security Checks
        if (refreshToken.IsRevoked)
        {
            // Reuse Detection: If a revoked token is used, revoke all descendant tokens (security breach suspected).
            // For now, simpler approach: Revoke all tokens for this user to force re-login.
            await RevokeAllUserTokensAsync(user.Id);
            return Result.Failure<AuthResponse>(
                new Error(
                    "Auth.InvalidTokenUsage",
                    "Invalid token usage detected. Please login again."
                )
            );
        }

        if (refreshToken.IsExpired)
            return Result.Failure<AuthResponse>(new Error("Auth.TokenExpired", "Token expired."));

        // Rotate Token
        var newAuthResponse = await CreateAndSaveAuthResponseAsync(user);

        // Revoke old token
        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.IsUsed = true;

        // _context.Update(refreshToken); // Redundant, change tracker handles it.
        await _context.SaveChangesAsync();

        return newAuthResponse;
    }

    public async Task<Result<bool>> RevokeTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.Token == token
        );

        if (refreshToken == null)
            return Result.Failure<bool>(new Error("Auth.TokenNotFound", "Token not found."));

        if (refreshToken.IsActive)
        {
            refreshToken.Revoked = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    // --- Helpers ---

    private (string AccessToken, RefreshToken RefreshToken) GenerateTokens(User user)
    {
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken(user.Id);
        return (accessToken, refreshToken);
    }

    private async Task<AuthResponse> CreateAndSaveAuthResponseAsync(User user)
    {
        var (accessToken, refreshToken) = GenerateTokens(user);
        user.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return new AuthResponse(accessToken, refreshToken.Token, refreshToken.Expires);
    }

    private string GenerateJwtToken(User user)
    {
        var secretKey = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(secretKey),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            // CreatedAt is set by default in BaseEntity
            UserId = userId,
        };

        return refreshToken;
    }

    private async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var tokens = await _context
            .RefreshTokens.Where(rt => rt.UserId == userId && rt.Revoked == null)
            .ToListAsync();
        foreach (var t in tokens)
        {
            t.Revoked = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}
