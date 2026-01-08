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
                    if (!result.Success)
                        return Results.BadRequest(result);
                    return Results.Ok(result);
                }
            )
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group
            .MapPost(
                "login",
                async (LoginRequest request, IAuthService authService) =>
                {
                    var result = await authService.LoginAsync(request);
                    if (!result.Success)
                        return Results.Json(result, statusCode: 401);
                    return Results.Ok(result);
                }
            )
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost(
            "refresh-token",
            async (RefreshTokenRequest request, IAuthService authService) =>
            {
                var result = await authService.RefreshTokenAsync(request);
                if (!result.Success)
                    return Results.BadRequest(result);
                return Results.Ok(result);
            }
        );

        group.MapPost(
            "revoke-token",
            async (RevokeTokenRequest request, IAuthService authService) =>
            {
                var result = await authService.RevokeTokenAsync(request.RefreshToken);
                if (!result.Success)
                    return Results.BadRequest(result);
                return Results.Ok(result);
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
