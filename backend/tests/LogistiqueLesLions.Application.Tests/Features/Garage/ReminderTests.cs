using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Garage;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Garage;

/// <summary>
/// Rappels de Mon Garage.
/// </summary>
/// <remarks>
/// Lo importante aquí es que el kilometraje <b>nunca se estima</b>: un recordatorio por
/// kilómetros solo vence cuando el usuario declara una lectura nueva.
/// </remarks>
public class ReminderTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ReminderService _service;

    private readonly AddReminderCommandHandler _add;
    private readonly UpdateReminderCommandHandler _update;
    private readonly SetReminderStatusCommandHandler _setStatus;
    private readonly DeleteReminderCommandHandler _delete;
    private readonly GetVehicleRemindersQueryHandler _list;
    private readonly GetUpcomingRemindersQueryHandler _upcoming;
    private readonly UpdateGarageVehicleCommandHandler _updateVehicle;
    private readonly AddMaintenanceRecordCommandHandler _addMaintenance;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();

    public ReminderTests()
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

        _context.VehicleMakes.Add(new VehicleMake { Id = _makeId, Name = "Toyota", Country = "JP" });
        _context.UserProfiles.AddRange(
            new UserProfile { Id = _userId, DisplayName = "Mamadou", Phone = "+221770000001", PasswordHash = "x" },
            new UserProfile { Id = _otherUserId, DisplayName = "Fatou", Phone = "+221770000002", PasswordHash = "x" });
        _context.GarageVehicles.Add(new GarageVehicle
        {
            Id = _vehicleId, UserId = _userId, MakeId = _makeId, Year = 2019, Mileage = 147_500
        });
        _context.SaveChanges();

        _service        = new ReminderService(_context);
        _add            = new AddReminderCommandHandler(_context, _service);
        _update         = new UpdateReminderCommandHandler(_context, _service);
        _setStatus      = new SetReminderStatusCommandHandler(_context);
        _delete         = new DeleteReminderCommandHandler(_context);
        _list           = new GetVehicleRemindersQueryHandler(_context);
        _upcoming       = new GetUpcomingRemindersQueryHandler(_context);
        _updateVehicle  = new UpdateGarageVehicleCommandHandler(_context, _service);
        _addMaintenance = new AddMaintenanceRecordCommandHandler(_context, _service);
    }

    private async Task<Guid> AddAsync(
        ReminderType type = ReminderType.Vidange,
        string label = "Prochaine vidange",
        DateTimeOffset? dueDate = null,
        int? dueMileage = 150_000)
    {
        var result = await _add.Handle(new AddReminderCommand(
            _userId, _vehicleId, new ReminderInput(type, label, dueDate, dueMileage, null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    /// <summary>Declara un kilometraje nuevo, que es lo único que hace avanzar los rappels por km.</summary>
    private Task<LogistiqueLesLions.Application.Common.Models.Result> DeclareMileageAsync(int mileage) =>
        _updateVehicle.Handle(new UpdateGarageVehicleCommand(_userId, _vehicleId,
            new GarageVehicleInput(_makeId, null, null, 2019, mileage,
                null, null, null, null, null, null, null, null, null, null)),
            CancellationToken.None);

    // ─── Crear ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaCrearElRappelComoAVenir()
    {
        var id = await AddAsync();

        var reminder = await _context.VehicleReminders.SingleAsync(r => r.Id == id);
        reminder.Status.Should().Be(ReminderStatus.AVenir);
        reminder.DueMileage.Should().Be(150_000);
        reminder.NotifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task SinFechaNiKilometrajeNoHayNadaQueVigilar()
    {
        var result = await _add.Handle(new AddReminderCommand(
            _userId, _vehicleId, new ReminderInput(ReminderType.Autre, "Vague", null, null, null)),
            CancellationToken.None);

        result.Error.Should().Be("Reminder.ConditionRequired");
    }

    [Fact]
    public async Task UnRappelPuedeNacerYaVencido()
    {
        // «La revisión que debía hacerse en mayo».
        await AddAsync(ReminderType.Revision, "Révision", DateTimeOffset.UtcNow.AddMonths(-2), null);

        var reminder = await _context.VehicleReminders.SingleAsync();
        reminder.Status.Should().Be(ReminderStatus.AFaire);
        reminder.NotifiedAt.Should().NotBeNull();
    }

    // ─── Vencimiento por fecha ─────────────────────────────────────────────

    [Fact]
    public async Task ElTrabajoEnSegundoPlanoDebePasarloAAFaireYAvisar()
    {
        await AddAsync(ReminderType.Assurance, "Assurance", DateTimeOffset.UtcNow.AddDays(5), null);

        // Todavía no toca.
        (await _service.EvaluateDueByDateAsync()).Should().Be(0);
        (await _context.UserNotifications.CountAsync()).Should().Be(0);

        // Pasa el tiempo.
        var reminder = await _context.VehicleReminders.SingleAsync();
        reminder.DueDate = DateTimeOffset.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        (await _service.EvaluateDueByDateAsync()).Should().Be(1);

        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AFaire);

        var notification = await _context.UserNotifications.SingleAsync();
        notification.UserId.Should().Be(_userId);
        notification.Category.Should().Be(NotificationCategories.Reminder);
        notification.Title.Should().Be("Assurance");
    }

    [Fact]
    public async Task NoDebeAvisarDosVecesDelMismoRappel()
    {
        await AddAsync(ReminderType.Assurance, "Assurance", DateTimeOffset.UtcNow.AddDays(-1), null);

        await _service.EvaluateDueByDateAsync();
        await _service.EvaluateDueByDateAsync();

        (await _context.UserNotifications.CountAsync()).Should().Be(1);
    }

    // ─── Vencimiento por kilometraje ───────────────────────────────────────

    [Fact]
    public async Task ElKilometrajeNoDebeEstimarseSolo()
    {
        // La especificación lo prohíbe: sin declaración del usuario, el rappel no avanza
        // por mucho tiempo que pase.
        await AddAsync(dueMileage: 150_000);

        await _service.EvaluateDueByDateAsync();

        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AVenir);
        (await _context.UserNotifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeclararUnKilometrajeNuevoDebeHacerVencerElRappel()
    {
        await AddAsync(dueMileage: 150_000);

        var result = await DeclareMileageAsync(151_200);
        result.IsSuccess.Should().BeTrue();

        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AFaire);

        var notification = await _context.UserNotifications.SingleAsync();
        notification.Category.Should().Be(NotificationCategories.Reminder);
        notification.Body.Should().Contain("150.000 km");
    }

    [Fact]
    public async Task UnKilometrajeInsuficienteNoDebeHacerVencerNada()
    {
        await AddAsync(dueMileage: 150_000);

        await DeclareMileageAsync(148_000);

        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AVenir);
    }

    [Fact]
    public async Task RegistrarUnaIntervencionTambienDeclaraKilometraje()
    {
        await AddAsync(dueMileage: 150_000);

        await _addMaintenance.Handle(new AddMaintenanceRecordCommand(
            _userId, _vehicleId, new MaintenanceInput(
                MaintenanceType.Pneus, DateTimeOffset.UtcNow.AddDays(-1), 152_000,
                "Pneus avant", null, null, null, null)),
            CancellationToken.None);

        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AFaire);
    }

    [Fact]
    public async Task ConFechaYKilometrajeDebeBastarConQueSeCumplaUnaCondicion()
    {
        // «Vidange — 15 décembre 2026 ou 150.000 km»: lo que llegue antes.
        await AddAsync(dueDate: DateTimeOffset.UtcNow.AddYears(1), dueMileage: 150_000);

        await DeclareMileageAsync(150_000);

        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AFaire);
    }

    // ─── Estados ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ElUsuarioDebePoderTerminarloYAnularlo()
    {
        var id = await AddAsync();

        await _setStatus.Handle(new SetReminderStatusCommand(_userId, id, ReminderStatus.Termine),
            CancellationToken.None);

        var reminder = await _context.VehicleReminders.SingleAsync();
        reminder.Status.Should().Be(ReminderStatus.Termine);
        reminder.CompletedAt.Should().NotBeNull();

        await _setStatus.Handle(new SetReminderStatusCommand(_userId, id, ReminderStatus.Annule),
            CancellationToken.None);

        reminder = await _context.VehicleReminders.SingleAsync();
        reminder.Status.Should().Be(ReminderStatus.Annule);
        reminder.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task ElUsuarioNoDebePoderMarcarloComoAFaire()
    {
        // Ese estado lo decide el sistema al cumplirse la condición.
        var id = await AddAsync();

        var result = await _setStatus.Handle(
            new SetReminderStatusCommand(_userId, id, ReminderStatus.AFaire), CancellationToken.None);

        result.Error.Should().Be("Reminder.StatusNotAllowed");
    }

    [Fact]
    public async Task AplazarloDebeDevolverloAAVenir()
    {
        var id = await AddAsync(dueMileage: 150_000);
        await DeclareMileageAsync(151_000);
        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AFaire);

        // Se aplaza a un kilometraje más lejano.
        await _update.Handle(new UpdateReminderCommand(_userId, id,
            new ReminderInput(ReminderType.Vidange, "Prochaine vidange", null, 160_000, null)),
            CancellationToken.None);

        (await _context.VehicleReminders.SingleAsync()).Status.Should().Be(ReminderStatus.AVenir);
    }

    // ─── Consultas ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaDecirCuantoFaltaSegunLaUltimaLecturaDeclarada()
    {
        await AddAsync(dueMileage: 150_000);

        var result = await _list.Handle(
            new GetVehicleRemindersQuery(_userId, _vehicleId), CancellationToken.None);

        // 150.000 - 147.500
        result.Value!.Single().MileageRemaining.Should().Be(2_500);
    }

    [Fact]
    public async Task LoQueTocaYaDebeIrPrimero()
    {
        await AddAsync(ReminderType.Assurance, "Assurance", DateTimeOffset.UtcNow.AddDays(30), null);
        await AddAsync(ReminderType.Revision, "Révision", DateTimeOffset.UtcNow.AddDays(-2), null);

        var result = await _upcoming.Handle(
            new GetUpcomingRemindersQuery(_userId), CancellationToken.None);

        result.Value!.First().Label.Should().Be("Révision");
        result.Value.First().Status.Should().Be(ReminderStatus.AFaire);
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoTerminadoNoDebeContarComoPendiente()
    {
        var id = await AddAsync();
        await _setStatus.Handle(new SetReminderStatusCommand(_userId, id, ReminderStatus.Termine),
            CancellationToken.None);

        var result = await _upcoming.Handle(
            new GetUpcomingRemindersQuery(_userId), CancellationToken.None);

        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task NadieMasDebeVerNiTocarLosRappels()
    {
        var id = await AddAsync();

        (await _list.Handle(new GetVehicleRemindersQuery(_otherUserId, _vehicleId), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _add.Handle(new AddReminderCommand(_otherUserId, _vehicleId,
            new ReminderInput(ReminderType.Autre, "x", DateTimeOffset.UtcNow, null, null)), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        (await _setStatus.Handle(new SetReminderStatusCommand(_otherUserId, id, ReminderStatus.Annule),
            CancellationToken.None)).Error.Should().Be("GarageVehicle.AccessDenied");

        (await _delete.Handle(new DeleteReminderCommand(_otherUserId, id), CancellationToken.None))
            .Error.Should().Be("GarageVehicle.AccessDenied");

        // Y no aparecen en su resumen.
        (await _upcoming.Handle(new GetUpcomingRemindersQuery(_otherUserId), CancellationToken.None))
            .Value!.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
