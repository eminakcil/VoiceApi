using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using VoiceApi.Features.Auth;
using VoiceApi.Features.Voice;
using VoiceApi.Infrastructure.Interceptors;
using VoiceApi.Infrastructure.Options;
using VoiceApi.Shared.Middlewares;

namespace VoiceApi.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataLayer(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Interceptors
        services.AddScoped<AuditSaveChangesInterceptor>();

        // DbContext
        services.AddDbContext<AppDbContext>(
            (sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
                options
                    .UseSqlite(configuration.GetConnectionString("DefaultConnection"))
                    .AddInterceptors(interceptor);
            }
        );

        // Redis or others if needed
        return services;
    }

    public static IServiceCollection AddAuthLayer(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Options
        services
            .AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        if (jwtSettings == null)
            throw new Exception("JwtSettings not configured");

        var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        // Validation Filter & related could be here or core
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Mapster
        VoiceApi.Shared.Utilities.MapsterConfig.Configure();

        // OpenAPI
        services.AddOpenApi();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVoiceService, VoiceService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAll",
                policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );
        });

        return services;
    }
}
