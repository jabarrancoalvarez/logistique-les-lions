using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetSimilarVehicles;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Vehicles;

/// <summary>
/// «Véhicules similaires»: comprueba el orden de prioridad de la especificación
/// (misma marca+modelo → precio similar → año similar → ubicación → carrocería).
/// </summary>
public class GetSimilarVehiclesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetSimilarVehiclesQueryHandler _handler;

    private readonly Guid _toyota = Guid.NewGuid();
    private readonly Guid _peugeot = Guid.NewGuid();
    private readonly Guid _rav4Model = Guid.NewGuid();
    private readonly Guid _seller = Guid.NewGuid();
    private Guid _referenceId;

    public GetSimilarVehiclesQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        _context = new ApplicationDbContext(
            options,
            new Infrastructure.Persistence.Interceptors.AuditInterceptor(mockCurrentUser.Object),
            new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
                mockCurrentUser.Object, new Microsoft.AspNetCore.Http.HttpContextAccessor()));

        _handler = new GetSimilarVehiclesQueryHandler(_context);
        Seed();
    }

    private void Seed()
    {
        _context.VehicleMakes.AddRange(
            new VehicleMake { Id = _toyota, Name = "Toyota", Country = "JP" },
            new VehicleMake { Id = _peugeot, Name = "Peugeot", Country = "FR" });
        _context.VehicleModels.Add(
            new VehicleModel { Id = _rav4Model, MakeId = _toyota, Name = "RAV4" });

        // Anuncio de referencia
        var reference = V("RAV4 référence", _toyota, _rav4Model, 8_900_000m, 2019, "DK", BodyType.Suv);
        _referenceId = reference.Id;

        // 1. Misma marca y modelo → máxima prioridad
        var sameModel = V("RAV4 même modèle", _toyota, _rav4Model, 15_000_000m, 2012, "ZG", BodyType.Suv);
        // 2. Misma marca, precio similar
        var sameMake = V("Toyota Hilux", _toyota, null, 9_200_000m, 2020, "TH", BodyType.PickUp);
        // 3. Precio y año similares, otra marca
        var samePrice = V("Peugeot 3008", _peugeot, null, 8_700_000m, 2018, "TH", BodyType.Suv);
        // 4. Misma región, precio muy distinto
        var sameRegion = V("Peugeot 208", _peugeot, null, 2_000_000m, 2010, "DK", BodyType.Citadine);
        // 5. Misma carrocería, sin nada más en común
        var sameBody = V("Peugeot 5008", _peugeot, null, 25_000_000m, 2024, "SL", BodyType.Suv);

        // No debe aparecer: no está publicado
        var draft = V("Toyota Yaris brouillon", _toyota, null, 8_800_000m, 2019, "DK", BodyType.Citadine);
        draft.Status = VehicleStatus.Brouillon;

        _context.Vehicles.AddRange(reference, sameModel, sameMake, samePrice, sameRegion, sameBody, draft);
        _context.SaveChanges();
    }

    private Vehicle V(string title, Guid makeId, Guid? modelId, decimal price, int year,
                      string region, BodyType body)
    {
        var id = Guid.NewGuid();
        return new Vehicle
        {
            Id = id,
            PublicReference = $"YU{Random.Shared.Next(10000, 99999)}",
            Slug = $"{id:N}",
            Title = title,
            MakeId = makeId,
            ModelId = modelId,
            Price = price,
            Year = year,
            Region = region,
            BodyType = body,
            SellerId = _seller,
            Status = VehicleStatus.Actif
        };
    }

    private async Task<List<string>> Similar(int take = 8)
    {
        var result = await _handler.Handle(
            new GetSimilarVehiclesQuery(_referenceId, take), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!.Select(v => v.Title).ToList();
    }

    [Fact]
    public async Task DeberiaRespetarElOrdenDePrioridad()
    {
        var titles = await Similar();

        // El del mismo modelo va primero aunque su precio y año se parezcan menos.
        titles[0].Should().Be("RAV4 même modèle");
        titles.Should().Contain("Toyota Hilux");
        titles.Should().Contain("Peugeot 3008");
    }

    [Fact]
    public async Task NoDeberiaIncluirseASiMismo()
    {
        (await Similar()).Should().NotContain("RAV4 référence");
    }

    [Fact]
    public async Task NoDeberiaIncluirAnunciosNoPublicados()
    {
        (await Similar()).Should().NotContain("Toyota Yaris brouillon");
    }

    [Fact]
    public async Task NoDeberiaRepetirVehiculosEntreCriterios()
    {
        var titles = await Similar();

        titles.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task DeberiaRespetarElLimiteSolicitado()
    {
        (await Similar(take: 2)).Should().HaveCount(2);
    }

    [Fact]
    public async Task DeberiaFallarSiElVehiculoNoExiste()
    {
        var result = await _handler.Handle(
            new GetSimilarVehiclesQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Vehicle.NotFound");
    }

    public void Dispose() => _context.Dispose();
}
