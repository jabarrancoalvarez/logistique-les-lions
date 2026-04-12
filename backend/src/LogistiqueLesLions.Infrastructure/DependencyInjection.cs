using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Infrastructure.Persistence;
using LogistiqueLesLions.Infrastructure.Persistence.Interceptors;
using LogistiqueLesLions.Infrastructure.Services;
using LogistiqueLesLions.Infrastructure.Services.Ai;
using LogistiqueLesLions.Infrastructure.Services.BackgroundJobs;
using LogistiqueLesLions.Infrastructure.Services.Email;
using LogistiqueLesLions.Infrastructure.Services.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiqueLesLions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ─── EF Core + PostgreSQL ────────────────────────────────────────────
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "vehicles");
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), null);
                });

            options.UseSnakeCaseNamingConvention();

            // En desarrollo: logging de queries lentas
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // ─── Redis ───────────────────────────────────────────────────────────
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "lll:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // ─── Servicios de infraestructura ────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IStorageService, LocalStorageService>();

        // ─── Email transaccional ─────────────────────────────────────────────
        // Provider seleccionado en runtime desde Email:Provider.
        // Si es Resend pero falta API key, hace fallback a Console (no rompe el arranque).
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var emailProvider = configuration["Email:Provider"] ?? "Console";
        var resendKey     = configuration["Email:Resend:ApiKey"];

        if (string.Equals(emailProvider, "Resend", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(resendKey))
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        }
        else
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        // ─── Webhook publisher (n8n / Slack / Zapier / Make) ────────────────
        services.Configure<WebhookOptions>(configuration.GetSection(WebhookOptions.SectionName));
        var webhooksEnabled = configuration.GetValue<bool>("Webhooks:Enabled");
        var webhookUrl      = configuration["Webhooks:Url"];
        if (webhooksEnabled && !string.IsNullOrWhiteSpace(webhookUrl))
        {
            services.AddHttpClient<IWebhookPublisher, HttpWebhookPublisher>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        }
        else
        {
            services.AddScoped<IWebhookPublisher, NoopWebhookPublisher>();
        }

        // ─── BackgroundService: detector de procesos atascados ──────────────
        services.Configure<StaleProcessOptions>(configuration.GetSection(StaleProcessOptions.SectionName));
        services.AddHostedService<StaleProcessNotifierService>();

        // ─── AI generativa (Anthropic Claude) ────────────────────────────────
        // Si no hay API key, fallback a NoopAiContentService para no romper desarrollo.
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        var anthropicKey = configuration["Anthropic:ApiKey"];
        if (!string.IsNullOrWhiteSpace(anthropicKey))
        {
            services.AddHttpClient<IAiContentService, ClaudeAiContentService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
        }
        else
        {
            services.AddScoped<IAiContentService, NoopAiContentService>();
        }

        return services;
    }
}
