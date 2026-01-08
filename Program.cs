using Scalar.AspNetCore;
using Serilog;
using VoiceApi.Features.Auth;
using VoiceApi.Features.Voice;
using VoiceApi.Infrastructure.Extensions;
using VoiceApi.Infrastructure.Options;
using VoiceApi.Shared.Middlewares;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// 1. Setup Serilog
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSignalR();
builder.Services.Configure<AzureSettings>(
    builder.Configuration.GetSection(AzureSettings.SectionName)
);

// 2. Add Services
builder
    .Services.AddDataLayer(builder.Configuration)
    .AddAuthLayer(builder.Configuration)
    .AddCoreServices();

var app = builder.Build();

// 3. Configure Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Scalar UI
}

app.UseGlobalExceptionHandler(); // Custom Exception Handler

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.UseStaticFiles();
app.MapHub<VoiceHub>("/hubs/voice");

Log.Information("--> Application is running on http://localhost:5200");

app.Run();
