using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.EstimateCost;

public class EstimateCostQueryHandler(IApplicationDbContext context)
    : IRequestHandler<EstimateCostQuery, Result<CostEstimateDto>>
{
    public async Task<Result<CostEstimateDto>> Handle(
        EstimateCostQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (vehicle is null)
            return Result<CostEstimateDto>.Failure("Vehículo no encontrado.");

        var req = await context.CountryRequirements
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.OriginCountry == request.OriginCountry &&
                r.DestinationCountry == request.DestinationCountry,
                cancellationToken);

        // Convertir precio a EUR si es necesario (simplificado: asumimos EUR)
        var vehiclePriceEur = vehicle.Price;

        if (req is null)
        {
            // Sin datos de normativa: estimación básica 10% arancel + 21% IVA
            var basicDuty = vehiclePriceEur * 0.10m;
            var basicVat  = (vehiclePriceEur + basicDuty) * 0.21m;
            var basicTotal = vehiclePriceEur + basicDuty + basicVat + 500m;
            return Result<CostEstimateDto>.Success(new CostEstimateDto(
                vehicle.Id, request.OriginCountry, request.DestinationCountry,
                vehiclePriceEur, basicDuty, basicVat, 500m, 0m, basicTotal,
                10m, 21m, false, 30,
                [
                    new("Precio del vehículo", vehiclePriceEur, null),
                    new("Arancel estimado (10%)", basicDuty, "Estimación sin datos de normativa"),
                    new("IVA estimado (21%)", basicVat, null),
                    new("Gestión y tramitación", 500m, "Costes administrativos estimados")
                ]
            ));
        }

        var customsDuty      = vehiclePriceEur * (req.CustomsRatePercent / 100m);
        var vatAmount        = (vehiclePriceEur + customsDuty) * (req.VatRatePercent / 100m);
        var processingFees   = req.EstimatedProcessingCostEur;

        // Coste de homologación: consultar HomologationRequirements
        var homologCost = 0m;
        if (req.HomologationRequired)
        {
            var homolog = await context.HomologationRequirements
                .AsNoTracking()
                .Where(h => h.DestinationCountry == request.DestinationCountry)
                .OrderByDescending(h => h.YearFrom)
                .FirstOrDefaultAsync(cancellationToken);
            homologCost = homolog?.EstimatedCostEur ?? 1500m;
        }

        var total = vehiclePriceEur + customsDuty + vatAmount + processingFees + homologCost;

        var lineItems = new List<CostLineItemDto>
        {
            new("Precio del vehículo", vehiclePriceEur, null),
            new($"Arancel aduanero ({req.CustomsRatePercent}%)", customsDuty,
                req.CustomsRatePercent == 0 ? "Intra-UE: arancel 0%" : null),
            new($"IVA importación ({req.VatRatePercent}%)", vatAmount, null),
            new("Gestión y tramitación documental", processingFees, null)
        };

        if (req.HomologationRequired)
            lineItems.Add(new("Homologación y adaptaciones técnicas", homologCost,
                "Coste estimado según vehículo y país de destino"));

        return Result<CostEstimateDto>.Success(new CostEstimateDto(
            vehicle.Id, request.OriginCountry, request.DestinationCountry,
            vehiclePriceEur, customsDuty, vatAmount, processingFees, homologCost, total,
            req.CustomsRatePercent, req.VatRatePercent, req.HomologationRequired,
            req.EstimatedDays,
            lineItems.AsReadOnly()
        ));
    }
}
