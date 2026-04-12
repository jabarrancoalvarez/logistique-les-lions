using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Compliance.Queries.EstimateCost;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.SimulateCost;

/// <summary>
/// Calculadora interactiva: estima costes a partir de un precio bruto sin
/// necesidad de un vehículo persistido en BBDD. Pensada para landing/wizard.
/// </summary>
public record SimulateCostQuery(
    decimal VehiclePrice,
    string OriginCountry,
    string DestinationCountry,
    bool HomologationOverride = false
) : IRequest<Result<CostEstimateDto>>;

public class SimulateCostQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SimulateCostQuery, Result<CostEstimateDto>>
{
    public async Task<Result<CostEstimateDto>> Handle(SimulateCostQuery request, CancellationToken ct)
    {
        if (request.VehiclePrice <= 0)
            return Result<CostEstimateDto>.Failure("Simulate.PriceRequired");

        var origin = request.OriginCountry?.ToUpperInvariant() ?? "";
        var dest   = request.DestinationCountry?.ToUpperInvariant() ?? "";
        if (origin.Length != 2 || dest.Length != 2)
            return Result<CostEstimateDto>.Failure("Simulate.CountryCodesInvalid");

        var price = request.VehiclePrice;

        var req = await db.CountryRequirements
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OriginCountry == origin && r.DestinationCountry == dest, ct);

        decimal customsRate, vatRate, processingFees, homologCost;
        bool homologRequired;
        int days;

        if (req is null)
        {
            customsRate    = 10m;
            vatRate        = 21m;
            processingFees = 500m;
            homologRequired = request.HomologationOverride;
            homologCost    = homologRequired ? 1500m : 0m;
            days           = 30;
        }
        else
        {
            customsRate     = req.CustomsRatePercent;
            vatRate         = req.VatRatePercent;
            processingFees  = req.EstimatedProcessingCostEur;
            homologRequired = req.HomologationRequired || request.HomologationOverride;
            days            = req.EstimatedDays;
            homologCost = 0m;
            if (homologRequired)
            {
                var h = await db.HomologationRequirements.AsNoTracking()
                    .Where(x => x.DestinationCountry == dest)
                    .OrderByDescending(x => x.YearFrom)
                    .FirstOrDefaultAsync(ct);
                homologCost = h?.EstimatedCostEur ?? 1500m;
            }
        }

        var customs = price * (customsRate / 100m);
        var vat     = (price + customs) * (vatRate / 100m);
        var total   = price + customs + vat + processingFees + homologCost;

        var lines = new List<CostLineItemDto>
        {
            new("Precio del vehículo", price, null),
            new($"Arancel aduanero ({customsRate}%)", customs,
                customsRate == 0 ? "Intra-UE: arancel 0%" : null),
            new($"IVA importación ({vatRate}%)", vat, null),
            new("Gestión y tramitación documental", processingFees, null)
        };
        if (homologRequired)
            lines.Add(new("Homologación", homologCost, "Estimación según país destino"));

        return Result<CostEstimateDto>.Success(new CostEstimateDto(
            Guid.Empty, origin, dest,
            price, customs, vat, processingFees, homologCost, total,
            customsRate, vatRate, homologRequired, days,
            lines.AsReadOnly()
        ));
    }
}
