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
        group.MapGet("/processes/{id:guid}.pdf", async (Guid id, IApplicationDbContext db, CancellationToken ct) =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var process = await db.ImportExportProcesses
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.TrackingCode,
                    Status = p.Status.ToString(),
                    ProcessType = p.ProcessType.ToString(),
                    p.OriginCountry,
                    p.DestinationCountry,
                    p.CompletionPercent,
                    p.EstimatedCostEur,
                    p.StartedAt,
                    p.CompletedAt,
                    VehicleTitle = p.Vehicle.Title
                })
                .FirstOrDefaultAsync(ct);

            if (process is null) return Results.NotFound();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(t => t.FontSize(11).FontColor(Colors.Grey.Darken3));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Logistique Les Lions").FontSize(18).Bold().FontColor(Colors.Black);
                        col.Item().Text("Resumen de proceso de tramitación").FontSize(12).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(16).Column(col =>
                    {
                        col.Spacing(8);
                        Row(col, "Código tracking:", process.TrackingCode);
                        Row(col, "Vehículo:",        process.VehicleTitle);
                        Row(col, "Estado:",          process.Status);
                        Row(col, "Tipo:",            process.ProcessType);
                        Row(col, "Origen → Destino:", $"{process.OriginCountry} → {process.DestinationCountry}");
                        Row(col, "Progreso:",        $"{process.CompletionPercent}%");
                        Row(col, "Coste estimado:",  process.EstimatedCostEur is null
                            ? "—"
                            : process.EstimatedCostEur.Value.ToString("C", CultureInfo.GetCultureInfo("es-ES")));
                        Row(col, "Iniciado:",        process.StartedAt?.ToString("dd/MM/yyyy") ?? "—");
                        Row(col, "Completado:",      process.CompletedAt?.ToString("dd/MM/yyyy") ?? "—");
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Documento generado el ");
                        t.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm 'UTC'"));
                        t.Span(" — logistiqueleslions.com");
                    });
                });
            }).GeneratePdf();

            return Results.File(pdfBytes, "application/pdf", $"proceso-{process.TrackingCode}.pdf");
        })
        .WithSummary("Generar PDF resumen de un proceso de tramitación");

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
