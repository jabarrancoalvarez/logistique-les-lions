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

        // Neon y muchos PaaS exponen la connection string en formato URI
        // (postgresql://user:pass@host/db). Npgsql solo acepta ADO.NET, así que
        // detectamos y convertimos aquí para que la app funcione con cualquiera
        // de los dos formatos sin tener que tocar Render.
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection");
        var connectionString = NormalizePostgresConnectionString(rawConnectionString);

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                connectionString,
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

    private static string? NormalizePostgresConnectionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            SslMode = Npgsql.SslMode.Require,
            TrustServerCertificate = true
        };

        // Neon suele requerir Channel Binding; lo respetamos si viene en el query.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2) continue;
            var key = Uri.UnescapeDataString(kv[0]);
            var value = Uri.UnescapeDataString(kv[1]);
            try { builder[key] = value; } catch { /* keyword desconocido: ignorar */ }
        }

        return builder.ConnectionString;
    }
}
