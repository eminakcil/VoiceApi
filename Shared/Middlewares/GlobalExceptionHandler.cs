using System.Net;
using System.Text.Json;
using Serilog;
using VoiceApi.Shared.Models;

namespace VoiceApi.Shared.Middlewares;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;

    public GlobalExceptionHandler(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled Exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = ApiResponse<string>.Fail(
            "An unexpected error occurred. Please try again later."
        );

        // In Development, you might want to show the actual exception message
        // if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        // {
        //     response.Message = exception.Message;
        // }

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}
