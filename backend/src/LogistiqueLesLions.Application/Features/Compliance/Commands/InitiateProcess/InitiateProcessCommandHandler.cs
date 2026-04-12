using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Commands.InitiateProcess;

public class InitiateProcessCommandHandler(
    IApplicationDbContext context,
    IWebhookPublisher webhooks)
    : IRequestHandler<InitiateProcessCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        InitiateProcessCommand request, CancellationToken cancellationToken)
    {
        var vehicleExists = await context.Vehicles
            .AnyAsync(v => v.Id == request.VehicleId, cancellationToken);
        if (!vehicleExists)
            return Result<Guid>.Failure("Vehículo no encontrado.");

        // Obtener los requisitos del par de países para generar el checklist
        var requirements = await context.CountryRequirements
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.OriginCountry == request.OriginCountry &&
                r.DestinationCountry == request.DestinationCountry,
                cancellationToken);

        // Calcular coste estimado
        var vehicle = await context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);

        decimal? estimatedCost = null;
        if (requirements is not null && vehicle is not null)
        {
            var duty   = vehicle.Price * (requirements.CustomsRatePercent / 100m);
            var vat    = (vehicle.Price + duty) * (requirements.VatRatePercent / 100m);
            estimatedCost = vehicle.Price + duty + vat + requirements.EstimatedProcessingCostEur;
        }

        var process = new ImportExportProcess
        {
            TrackingCode       = ImportExportProcess.GenerateTrackingCode(),
            VehicleId          = request.VehicleId,
            BuyerId            = request.BuyerId,
            SellerId           = request.SellerId,
            OriginCountry      = request.OriginCountry,
            DestinationCountry = request.DestinationCountry,
            ProcessType        = request.ProcessType,
            Status             = ProcessStatus.InProgress,
            EstimatedCostEur   = estimatedCost,
            StartedAt          = DateTimeOffset.UtcNow
        };

        context.ImportExportProcesses.Add(process);

        // Generar checklist de documentos según la normativa
        if (requirements is not null)
        {
            var docTypes = requirements.GetDocumentTypes();
            foreach (var docTypeStr in docTypes)
            {
                // Obtener plantilla si existe (DocumentTemplate.DocumentType es string)
                var template = await context.DocumentTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.Country == request.DestinationCountry &&
                        t.DocumentType == docTypeStr,
                        cancellationToken);

                process.Documents.Add(new ProcessDocument
                {
                    ProcessId        = process.Id,
                    DocumentType     = docTypeStr,
                    Country          = request.DestinationCountry,
                    Status           = DocumentStatus.Pending,
                    ResponsibleParty = DetermineResponsible(docTypeStr),
                    RequiredByDate   = DateTimeOffset.UtcNow.AddDays(requirements.EstimatedDays),
                    TemplateUrl      = template?.TemplateUrl,
                    OfficialUrl      = template?.OfficialUrl,
                    InstructionsEs   = template?.InstructionsEs,
                    EstimatedCostEur = template?.EstimatedCostEur
                });
            }
        }

        process.RecalculateCompletion();
        await context.SaveChangesAsync(cancellationToken);

        // Notificar a sistemas externos (n8n/Slack/email vía webhook).
        await webhooks.PublishAsync("process.created", new
        {
            id                 = process.Id,
            trackingCode       = process.TrackingCode,
            vehicleId          = process.VehicleId,
            buyerId            = process.BuyerId,
            sellerId           = process.SellerId,
            originCountry      = process.OriginCountry,
            destinationCountry = process.DestinationCountry,
            processType        = process.ProcessType.ToString(),
            estimatedCostEur   = process.EstimatedCostEur,
            documentsCount     = process.Documents.Count,
            startedAt          = process.StartedAt
        }, cancellationToken);

        return Result<Guid>.Success(process.Id);
    }

    private static string DetermineResponsible(string docTypeStr) => docTypeStr switch
    {
        "TituloPropiedad"   => "seller",
        "FichaTecnica"      => "seller",
        "Itv"               => "seller",
        "COC"               => "seller",
        "DeclaracionAduana" => "buyer",
        "PagoAranceles"     => "buyer",
        "Homologacion"      => "buyer",
        _                   => "buyer"
    };
}
