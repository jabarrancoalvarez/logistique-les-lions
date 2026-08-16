using System.Globalization;
using System.Text;
using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>
/// Exportaciones administrativas: CSV de vehículos y PDF de albarán por proceso.
/// </summary>
public static class ExportEndpoints
{
    public static RouteGroupBuilder MapExportEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization("CanViewAdminPanel");

        // GET /api/v1/exports/vehicles.csv
        group.MapGet("/vehicles.csv", async (IApplicationDbContext db, CancellationToken ct) =>
        {
            var vehicles = await db.Vehicles
                .AsNoTracking()
                .Select(v => new
                {
                    v.Id,
                    v.Title,
                    v.Slug,
                    Status = v.Status.ToString(),
                    v.Price,
                    v.Currency,
                    v.Year,
                    v.Mileage,
                    v.CreatedAt
                })
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine("id,title,slug,status,price,currency,year,mileage,created_at");
            foreach (var v in vehicles)
            {
                sb.Append(v.Id).Append(',')
                  .Append(Escape(v.Title)).Append(',')
                  .Append(Escape(v.Slug)).Append(',')
                  .Append(v.Status).Append(',')
                  .Append(v.Price.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(v.Currency).Append(',')
                  .Append(v.Year).Append(',')
                  .Append(v.Mileage).Append(',')
                  .Append(v.CreatedAt.ToString("O", CultureInfo.InvariantCulture))
                  .AppendLine();
            }

            return Results.File(
                Encoding.UTF8.GetBytes(sb.ToString()),
                contentType: "text/csv; charset=utf-8",
                fileDownloadName: $"vehicles-{DateTime.UtcNow:yyyyMMdd}.csv");
        })
        .WithSummary("Exportar listado de vehículos a CSV");

        // GET /api/v1/exports/processes/{id}.pdf
        // ❌ Se retiró «/processes/{id}.pdf», el albarán del producto anterior: iba con
        // los procesos de tramitación. El CSV de vehículos se conserva porque puede
        // reutilizarse en Statistiques.

        return group;
    }

    private static void Row(QuestPDF.Infrastructure.IContainer _, string label, string value) { }
    private static void Row(QuestPDF.Fluent.ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(150).Text(label).SemiBold();
            row.RelativeItem().Text(value);
        });
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
