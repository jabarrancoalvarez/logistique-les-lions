using Scalar.AspNetCore;
using LogistiqueLesLions.API.Endpoints;
using LogistiqueLesLions.API.Middleware;
using LogistiqueLesLions.Application;
using LogistiqueLesLions.Infrastructure;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;

// ─── Serilog bootstrap (antes del host) ────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("🦁 Iniciando Logistique Les Lions API...");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog (configuración completa desde appsettings) ────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "LogistiqueLesLions.API")
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // ─── JSON (Minimal API) — enums como strings ───────────────────────────────
    // Sin esto, el model binder rechaza { "condition": "Used" } con 400 (body
    // vacío) porque espera un entero. Afecta a todos los endpoints que reciben
    // DTOs con enums del dominio.
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    // ─── Application + Infrastructure ──────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddSingleton<LogistiqueLesLions.API.Telemetry.BusinessMetrics>();
    builder.Services.AddScoped<LogistiqueLesLions.Application.Common.Interfaces.INotificationService,
                               LogistiqueLesLions.API.Hubs.SignalRNotificationService>();
    builder.Services.AddScoped<LogistiqueLesLions.Infrastructure.Persistence.Seeding.DatabaseSeeder>();

    // ─── OpenTelemetry ──────────────────────────────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("LogistiqueLesLions.API"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("LogistiqueLesLions.Business")
            .AddPrometheusExporter());

    // ─── OpenAPI / Scalar ───────────────────────────────────────────────────────
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Info = new()
            {
                Title = "Logistique Les Lions — API",
                Version = "v1",
                Description = "Plataforma SaaS de compraventa internacional de vehículos con gestión documental transfronteriza."
            };
            return Task.CompletedTask;
        });
    });

    // ─── CORS ───────────────────────────────────────────────────────────────────
    // Acepta tanto array (appsettings.json) como string CSV (env var en Render)
    var allowedOriginsRaw = builder.Configuration["Cors:AllowedOrigins"];
    string[] allowedOrigins;
    if (!string.IsNullOrWhiteSpace(allowedOriginsRaw))
    {
        allowedOrigins = allowedOriginsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
    else
    {
        allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:4200"];
    }

    Log.Information("CORS: orígenes permitidos → {Origins}", string.Join(", ", allowedOrigins));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LllCorsPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("X-Correlation-Id", "X-Total-Count");
        });
    });

    // ─── Rate Limiting ──────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        // Límite general por IP: 100 requests/minuto
        options.AddFixedWindowLimiter("IpRateLimit", limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 10;
        });

        // Límite estricto para endpoints de auth: 10 intentos/5min
        options.AddFixedWindowLimiter("AuthRateLimit", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(5);
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.Headers["Retry-After"] = "60";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { error = "Demasiadas solicitudes. Inténtalo en 60 segundos." }, ct);
        };
    });

    // ─── Auth (JWT Bearer) ──────────────────────────────────────────────────────
    // ─── SignalR ────────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // SignalR/WebSocket: los navegadores no permiten cabeceras custom en
            // handshakes WS, así que el cliente manda el JWT como ?access_token=.
            // Este handler lo extrae SOLO para rutas de hubs (resto usa el header).
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    // ─── Authorization policies ─────────────────────────────────────────────────
    // Roles del dominio (UserRole enum): Buyer, Seller, Dealer, Admin, Moderator.
    // Política composite: agrupar roles por capacidad para no hardcodear nombres
    // de rol en cada endpoint.
    builder.Services.AddAuthorization(options =>
    {
        // Roles individuales
        options.AddPolicy("AdminOnly",     p => p.RequireRole("Admin"));
        options.AddPolicy("ModeratorOnly", p => p.RequireRole("Moderator"));
        options.AddPolicy("DealerOnly",    p => p.RequireRole("Dealer"));

        // Capacidades — usar éstas en endpoints en lugar de roles directos
        options.AddPolicy("CanModerate",       p => p.RequireRole("Admin", "Moderator"));
        options.AddPolicy("CanPublishVehicle", p => p.RequireRole("Admin", "Dealer", "Seller"));
        options.AddPolicy("CanManageUsers",    p => p.RequireRole("Admin"));
        options.AddPolicy("CanViewAdminPanel", p => p.RequireRole("Admin", "Moderator"));
        options.AddPolicy("CanBuyVehicle",     p => p.RequireRole("Admin", "Dealer", "Buyer"));
    });

    // ─── Health checks ──────────────────────────────────────────────────────────
    // live  → el proceso responde (kubelet/Render restart trigger)
    // ready → dependencias listas (DB) — el LB no enruta tráfico hasta que pase
    //
    // Usamos AddDbContextCheck en lugar de AddNpgSql porque la connection string
    // de Neon incluye "Channel Binding=Require", que el parser interno de
    // HealthChecks.NpgSql no entendía y rompía el /health en producción. El
    // DbContext ya tiene Npgsql.EFCore configurado correctamente.
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(
            name: "postgres",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            tags: ["ready", "db"]);

    // ────────────────────────────────────────────────────────────────────────────
    var app = builder.Build();
    // ────────────────────────────────────────────────────────────────────────────

    // ─── Aplicar migraciones automáticamente (excepto en design-time) ──
    if (Environment.GetEnvironmentVariable("EF_DESIGN_TIME") != "1")
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            Log.Information("✓ Migraciones aplicadas correctamente");

            if (builder.Configuration.GetValue<bool>("Seed:Enabled"))
            {
                var seeder = scope.ServiceProvider
                    .GetRequiredService<LogistiqueLesLions.Infrastructure.Persistence.Seeding.DatabaseSeeder>();
                await seeder.SeedAsync();
                Log.Information("✓ Seed de datos demo aplicado");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudieron aplicar migraciones automáticas");
            throw;
        }
    }

    // ─── Pipeline de middleware ─────────────────────────────────────────────────
    // IMPORTANTE: UseCors debe ir ANTES del ExceptionHandler para que las respuestas
    // de error (500) también incluyan los headers Access-Control-Allow-Origin.
    // Si no, el navegador reporta "CORS blocked" cuando en realidad hay un 500.
    app.UseCors("LllCorsPolicy");
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlerMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0000}ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        };
    });

    // ─── Archivos estáticos (uploads locales) ────────────────────────────────
    var uploadsPath = Path.Combine(app.Environment.ContentRootPath,
        app.Configuration["Storage:LocalPath"] ?? "uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath  = "/uploads"
    });

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ─── Security headers ────────────────────────────────────────────────────────
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        if (!context.Request.IsHttps && !app.Environment.IsDevelopment())
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        await next();
    });

    // ─── OpenAPI ──────────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title = "Logistique Les Lions — API Docs";
            opts.Theme = Scalar.AspNetCore.ScalarTheme.DeepSpace;
        });
    }

    // ─── Health ────────────────────────────────────────────────────────────────
    // /health        → todos los checks (compatibilidad legacy)
    // /health/live   → solo proceso vivo (sin DB) — para liveness probe
    // /health/ready  → checks con tag "ready" (incluye DB) — para readiness probe
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // ─── Prometheus metrics scrape endpoint ────────────────────────────────────
    app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();

    // ─── Endpoints Minimal API ─────────────────────────────────────────────────
    var v1 = app.MapGroup("/api/v1")
        .RequireRateLimiting("IpRateLimit");

    v1.MapGroup("/vehicles")
        .WithTags("Vehículos")
        .MapVehicleEndpoints();

    v1.MapGroup("/vehicles")
        .WithTags("Vehículos")
        .MapVehicleCrudEndpoints();

    v1.MapGroup("/compliance")
        .WithTags("Tramitación / Compliance")
        .MapComplianceEndpoints();

    v1.MapGroup("/countries")
        .WithTags("Países")
        .MapCountryEndpoints();

    v1.MapGroup("/auth")
        .WithTags("Autenticación")
        .MapAuthEndpoints();

    v1.MapGroup("/messaging")
        .WithTags("Mensajería")
        .MapMessagingEndpoints();

    v1.MapGroup("/admin")
        .WithTags("Administración")
        .MapAdminEndpoints();

    v1.MapGroup("/newsletter")
        .WithTags("Newsletter")
        .MapNewsletterEndpoints();

    // Endpoint público sin autenticación — tracking por código
    v1.MapGroup("/public/tracking")
        .WithTags("Tracking público")
        .MapPublicTrackingEndpoints();

    v1.MapGroup("/exports")
        .WithTags("Exportaciones")
        .MapExportEndpoints();

    v1.MapGroup("/notifications")
        .WithTags("Notificaciones")
        .MapNotificationsEndpoints();

    v1.MapGroup("/marketplace")
        .WithTags("Marketplace partners")
        .MapMarketplaceEndpoints();

    // SignalR hubs
    app.MapHub<LogistiqueLesLions.API.Hubs.ChatHub>("/hubs/chat")
        .RequireAuthorization();
    app.MapHub<LogistiqueLesLions.API.Hubs.NotificationHub>("/hubs/notifications")
        .RequireAuthorization();

    // ─── Redirect raíz a docs ────────────────────────────────────────────────
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).AllowAnonymous();

    Log.Information("🚀 API lista en {Urls}", string.Join(", ", app.Urls));
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "La aplicación terminó inesperadamente");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
