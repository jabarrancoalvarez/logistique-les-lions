using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LogistiqueLesLions.Infrastructure.Persistence;

/// <summary>
/// Factory usado exclusivamente por las herramientas de diseño de EF Core
/// (dotnet ef migrations add / database update) para crear el contexto
/// sin ejecutar el startup completo de la API.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Leer config desde el proyecto API (appsettings + appsettings.Development.json)
        var apiDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "LogistiqueLesLions.API");

        var config = new ConfigurationBuilder()
            .SetBasePath(apiDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not found.");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        // Los interceptores no se necesitan en design-time
        return new ApplicationDbContext(
            optionsBuilder.Options,
            new Interceptors.AuditInterceptor(null!),
            new Interceptors.AuditLogInterceptor(null!, null!));
    }
}
