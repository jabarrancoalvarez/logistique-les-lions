using Scalar.AspNetCore;
using LogistiqueLesLions.API.Endpoints;
using LogistiqueLesLions.API.Middleware;
using LogistiqueLesLions.Application;
using LogistiqueLesLions.Infrastructure;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

    // ─── Application + Infrastructure ──────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ─── OpenTelemetry ──────────────────────────────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("LogistiqueLesLions.API"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

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
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:4200"];

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
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });

    // ─── Health checks ──────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

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
        }
        catch (Exception ex)
        {
            Log.Warning("No se pudieron aplicar migraciones automáticas: {Message}", ex.Message);
        }
    }

    // ─── Pipeline de middleware ─────────────────────────────────────────────────
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

    app.UseCors("LllCorsPolicy");
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
    app.MapHealthChecks("/health");

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

    // SignalR hub
    app.MapHub<LogistiqueLesLions.API.Hubs.ChatHub>("/hubs/chat")
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
