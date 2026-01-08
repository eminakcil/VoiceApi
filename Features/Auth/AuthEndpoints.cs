using Microsoft.AspNetCore.Mvc;
using VoiceApi.Features.Auth;
using VoiceApi.Shared.Models;
using VoiceApi.Shared.Utilities;

namespace VoiceApi.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/auth");

        group
            .MapPost(
                "register",
                async (RegisterRequest request, IAuthService authService) =>
                {
                    var result = await authService.RegisterAsync(request);
                    if (result.IsFailure)
                        return Results.BadRequest(
                            ApiResponse<AuthResponse>.Fail(result.Error.Message)
                        );
                    return Results.Ok(
                        ApiResponse<AuthResponse>.Ok(result.Value, "User registered successfully.")
                    );
                }
            )
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group
            .MapPost(
                "login",
                async (LoginRequest request, IAuthService authService) =>
                {
                    var result = await authService.LoginAsync(request);
                    if (result.IsFailure)
                        return Results.Json(
                            ApiResponse<AuthResponse>.Fail(result.Error.Message),
                            statusCode: 401
                        );
                    return Results.Ok(
                        ApiResponse<AuthResponse>.Ok(result.Value, "Login successful.")
                    );
                }
            )
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost(
            "refresh-token",
            async (RefreshTokenRequest request, IAuthService authService) =>
            {
                var result = await authService.RefreshTokenAsync(request);
                if (result.IsFailure)
                    return Results.BadRequest(ApiResponse<AuthResponse>.Fail(result.Error.Message));
                return Results.Ok(ApiResponse<AuthResponse>.Ok(result.Value, "Token refreshed."));
            }
        );

        group.MapPost(
            "revoke-token",
            async (RevokeTokenRequest request, IAuthService authService) =>
            {
                var result = await authService.RevokeTokenAsync(request.RefreshToken);
                if (result.IsFailure)
                    return Results.BadRequest(ApiResponse<bool>.Fail(result.Error.Message));
                return Results.Ok(ApiResponse<bool>.Ok(result.Value, "Token revoked."));
            }
        );

        group
            .MapGet(
                "check-auth",
                () =>
                {
                    return Results.Ok(new ApiResponse<string>("You are authorized!"));
                }
            )
            .RequireAuthorization(); // Requires default policy or authenticated user
    }
}
