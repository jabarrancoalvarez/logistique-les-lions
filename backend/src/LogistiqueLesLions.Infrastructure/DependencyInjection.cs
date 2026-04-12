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

        Npgsql.NpgsqlConnectionStringBuilder builder;

        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            // Formato URI (el que devuelve Neon por defecto) → convertir
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':', 2);

            builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
                SslMode = Npgsql.SslMode.Require,
                TrustServerCertificate = true
            };

            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length != 2) continue;
                var key = Uri.UnescapeDataString(kv[0]);
                var value = Uri.UnescapeDataString(kv[1]);

                // Channel binding lo gestionamos explícitamente más abajo
                if (IsChannelBindingKey(key)) continue;

                try { builder[key] = value; } catch { /* keyword desconocido */ }
            }
        }
        else
        {
            // Ya está en formato ADO.NET. Algunas versiones de Npgsql no reconocen
            // "Channel Binding" como keyword y el constructor lanza si está presente,
            // así que lo eliminamos del string antes de parsear.
            var sanitized = string.Join(';', raw
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Where(part =>
                {
                    var eq = part.IndexOf('=');
                    if (eq <= 0) return true;
                    var key = part.Substring(0, eq).Trim();
                    return !IsChannelBindingKey(key);
                }));
            builder = new Npgsql.NpgsqlConnectionStringBuilder(sanitized);
        }

        // IMPORTANTE: con TrustServerCertificate=true, Npgsql no puede obtener el
        // token tls-server-end-point del canal TLS, así que SCRAM-PLUS (channel
        // binding) falla con "Couldn't set channel binding". Forzamos SslMode=Require
        // + TrustServerCertificate=true (Neon requiere TLS pero nos basta con
        // validar el hostname) y deshabilitamos channel binding explícitamente.
        builder.SslMode = Npgsql.SslMode.Require;
        builder.TrustServerCertificate = true;
        builder.ChannelBinding = Npgsql.ChannelBinding.Disable;

        return builder.ConnectionString;
    }

    private static bool IsChannelBindingKey(string key)
    {
        var normalized = key.Replace(" ", "").Replace("_", "");
        return normalized.Equals("channelbinding", StringComparison.OrdinalIgnoreCase);
    }
}
