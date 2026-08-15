using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Admin.Users;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Admin;

/// <summary>
/// «Gestion des utilisateurs» del backoffice.
/// </summary>
/// <remarks>
/// La regla de la especificación es que el administrador no pueda tocar información
/// sensible <b>sin dejar trazabilidad</b>: cada restricción de cuenta exige un motivo y
/// deja una fila que nadie puede modificar después.
/// </remarks>
public class AdminUserTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetAdminUsersQueryHandler _list;
    private readonly GetAdminUserQueryHandler _detail;
    private readonly ChangeAccountStatusCommandHandler _status;
    private readonly AddAdminNoteCommandHandler _addNote;
    private readonly DeleteAdminNoteCommandHandler _deleteNote;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _otherAdminId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _proId = Guid.NewGuid();
    private readonly Guid _makeId = Guid.NewGuid();

    public AdminUserTests()
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
            new UserProfile
            {
                Id = _adminId, DisplayName = "Admin", Phone = "+221770000000",
                PasswordHash = "x", Role = UserRole.Admin
            },
            new UserProfile
            {
                Id = _otherAdminId, DisplayName = "Autre admin", Phone = "+221770000009",
                PasswordHash = "x", Role = UserRole.Admin
            },
            new UserProfile
            {
                Id = _userId, DisplayName = "Mamadou Diop", Phone = "+221770000001",
                Email = "mamadou@example.sn", PasswordHash = "x", City = "Dakar",
                AccountType = AccountType.Particulier, PhoneVerified = true
            },
            new UserProfile
            {
                Id = _proId, DisplayName = "Auto Dakar", Phone = "+221770000002",
                PasswordHash = "x", City = "Thiès", AccountType = AccountType.Professionnel
            });
        _context.SaveChanges();

        _list       = new GetAdminUsersQueryHandler(_context);
        _detail     = new GetAdminUserQueryHandler(_context);
        _status     = new ChangeAccountStatusCommandHandler(_context);
        _addNote    = new AddAdminNoteCommandHandler(_context);
        _deleteNote = new DeleteAdminNoteCommandHandler(_context);
    }

    // ─── Listado ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaBuscarPorNombreTelefonoOCorreo()
    {
        foreach (var term in new[] { "mamadou", "770000001", "example.sn" })
        {
            var result = await _list.Handle(new GetAdminUsersQuery(Search: term), CancellationToken.None);

            result.Value!.Items.Should().ContainSingle($"«{term}» debe encontrar a Mamadou")
                .Which.Id.Should().Be(_userId);
        }
    }

    [Fact]
    public async Task DeberiaFiltrarPorCiudadTipoDeCuentaYVerificacion()
    {
        (await _list.Handle(new GetAdminUsersQuery(City: "dakar"), CancellationToken.None))
            .Value!.Items.Should().ContainSingle().Which.Id.Should().Be(_userId);

        (await _list.Handle(
            new GetAdminUsersQuery(AccountType: AccountType.Professionnel), CancellationToken.None))
            .Value!.Items.Should().ContainSingle().Which.Id.Should().Be(_proId);

        (await _list.Handle(new GetAdminUsersQuery(PhoneVerified: true), CancellationToken.None))
            .Value!.Items.Should().ContainSingle().Which.Id.Should().Be(_userId);
    }

    [Fact]
    public async Task DeberiaContarLosAnunciosDeCadaUsuario()
    {
        _context.Vehicles.AddRange(
            Listing(_proId, VehicleStatus.Actif),
            Listing(_proId, VehicleStatus.Brouillon));
        await _context.SaveChangesAsync();

        var result = await _list.Handle(
            new GetAdminUsersQuery(AccountType: AccountType.Professionnel), CancellationToken.None);

        // También los borradores: son anuncios suyos aunque no se vean.
        result.Value!.Items.Single().ListingsCount.Should().Be(2);
    }

    // ─── Estado de la cuenta ───────────────────────────────────────────────

    [Fact]
    public async Task SuspenderExigeMotivoYFechaDeFinal()
    {
        (await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Suspended, null,
            DateTimeOffset.UtcNow.AddDays(7)), CancellationToken.None))
            .Error.Should().Be("Admin.ReasonRequired");

        // Una suspensión sin final es un bloqueo con otro nombre.
        (await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Suspended, "Comportement inapproprié", null),
            CancellationToken.None))
            .Error.Should().Be("Admin.SuspensionEndRequired");

        (await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Suspended, "Comportement inapproprié",
            DateTimeOffset.UtcNow.AddDays(-1)), CancellationToken.None))
            .Error.Should().Be("Admin.SuspensionEndInPast");
    }

    [Fact]
    public async Task SuspenderDebeImpedirElAccesoHastaLaFecha()
    {
        var until = DateTimeOffset.UtcNow.AddDays(7);

        var result = await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Suspended, "Comportement inapproprié", until),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await _context.UserProfiles.SingleAsync(u => u.Id == _userId);
        user.Status.Should().Be(AccountStatus.Suspended);
        user.SuspendedUntil.Should().BeCloseTo(until, TimeSpan.FromSeconds(1));
        user.CanSignIn.Should().BeFalse();
    }

    [Fact]
    public async Task UnaSuspensionCumplidaDebeDejarVolverAEntrar()
    {
        var user = await _context.UserProfiles.SingleAsync(u => u.Id == _userId);
        user.Status = AccountStatus.Suspended;
        user.SuspendedUntil = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();

        // No hace falta que nadie se acuerde de levantarla.
        user.CanSignIn.Should().BeTrue();
    }

    [Fact]
    public async Task BloquearNoDebeDejarFechaDeFinal()
    {
        await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Suspended, "Avertissement",
            DateTimeOffset.UtcNow.AddDays(3)), CancellationToken.None);

        await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Blocked, "Tentative de fraude"),
            CancellationToken.None);

        var user = await _context.UserProfiles.SingleAsync(u => u.Id == _userId);
        user.Status.Should().Be(AccountStatus.Blocked);
        user.SuspendedUntil.Should().BeNull();
        user.CanSignIn.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivarNoExigeMotivo()
    {
        await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Blocked, "Tentative de fraude"), CancellationToken.None);

        var result = await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Active, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.UserProfiles.SingleAsync(u => u.Id == _userId))
            .CanSignIn.Should().BeTrue();
    }

    [Fact]
    public async Task CadaCambioDeEstadoDebeDejarRastro()
    {
        await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Blocked, "Tentative de fraude"), CancellationToken.None);
        await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Active, "Vérification effectuée"), CancellationToken.None);

        var detail = await _detail.Handle(new GetAdminUserQuery(_userId), CancellationToken.None);

        var actions = detail.Value!.Actions;
        actions.Should().HaveCount(2);
        // Lo más reciente primero, y con el motivo escrito.
        actions[0].Type.Should().Be(AdminActionType.AccountActivated);
        actions[1].Type.Should().Be(AdminActionType.AccountBlocked);
        actions[1].Reason.Should().Be("Tentative de fraude");
        actions[1].AdminName.Should().Be("Admin");
    }

    [Fact]
    public async Task UnAdministradorNoDebePoderTocarseASiMismo()
    {
        var result = await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _adminId, AccountStatus.Blocked, "…"), CancellationToken.None);

        result.Error.Should().Be("Admin.CannotActOnSelf");
    }

    [Fact]
    public async Task UnAdministradorNoDebePoderBloquearAOtro()
    {
        // La gestión de administradores no se hace desde esta pantalla.
        var result = await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _otherAdminId, AccountStatus.Blocked, "…"), CancellationToken.None);

        result.Error.Should().Be("Admin.CannotActOnAdmin");
    }

    // ─── Ficha ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LaFichaDebeResumirLaActividadDelUsuario()
    {
        var sold = Listing(_userId, VehicleStatus.Vendu, DateTimeOffset.UtcNow.AddDays(-10));
        var active = Listing(_userId, VehicleStatus.Actif, DateTimeOffset.UtcNow.AddDays(-3));
        _context.Vehicles.AddRange(sold, active);
        _context.GarageVehicles.Add(new GarageVehicle
        {
            UserId = _userId, MakeId = _makeId, Year = 2019
        });
        await _context.SaveChangesAsync();

        var detail = await _detail.Handle(new GetAdminUserQuery(_userId), CancellationToken.None);

        var activity = detail.Value!.Activity;
        activity.ListingsPublished.Should().Be(2);
        activity.ListingsSold.Should().Be(1);
        // De Mon Garage solo se dice cuántos: su contenido es privado.
        activity.GarageVehicles.Should().Be(1);
    }

    // ─── Notas internas ────────────────────────────────────────────────────

    [Fact]
    public async Task DeberiaPoderAnotarseContextoSobreUnUsuario()
    {
        var result = await _addNote.Handle(new AddAdminNoteCommand(
            _adminId, AdminTargetType.User, _userId, "Appelé le 12/08, tout est en ordre."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var detail = await _detail.Handle(new GetAdminUserQuery(_userId), CancellationToken.None);
        detail.Value!.Notes.Should().ContainSingle()
            .Which.Body.Should().Be("Appelé le 12/08, tout est en ordre.");
    }

    [Fact]
    public async Task NoDebeAnotarseSobreAlguienQueNoExiste()
    {
        var result = await _addNote.Handle(new AddAdminNoteCommand(
            _adminId, AdminTargetType.User, Guid.NewGuid(), "…"), CancellationToken.None);

        result.Error.Should().Be("Admin.TargetNotFound");
    }

    [Fact]
    public async Task CadaAdministradorRetiraSusPropiasNotas()
    {
        var note = await _addNote.Handle(new AddAdminNoteCommand(
            _adminId, AdminTargetType.User, _userId, "Note interne"), CancellationToken.None);

        (await _deleteNote.Handle(
            new DeleteAdminNoteCommand(_otherAdminId, note.Value), CancellationToken.None))
            .Error.Should().Be("Admin.NotNoteAuthor");

        (await _deleteNote.Handle(
            new DeleteAdminNoteCommand(_adminId, note.Value), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await _detail.Handle(new GetAdminUserQuery(_userId), CancellationToken.None))
            .Value!.Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task LasAccionesNoDebenPoderBorrarse()
    {
        await _status.Handle(new ChangeAccountStatusCommand(
            _adminId, _userId, AccountStatus.Blocked, "Tentative de fraude"), CancellationToken.None);

        // El registro es append-only: no existe comando para retirarlo, y la entidad no
        // se filtra por soft delete.
        var action = await _context.AdminActions.SingleAsync();
        action.DeletedAt.Should().BeNull();
        (await _context.AdminActions.CountAsync()).Should().Be(1);
    }

    // ─── Ayudas ────────────────────────────────────────────────────────────

    private Vehicle Listing(Guid sellerId, VehicleStatus status, DateTimeOffset? publishedAt = null) => new()
    {
        PublicReference = $"YU{Guid.NewGuid().ToString()[..5]}",
        Slug = $"annonce-{Guid.NewGuid()}",
        Title = "Toyota RAV4",
        MakeId = _makeId,
        Year = 2019,
        Price = 8_900_000m,
        SellerId = sellerId,
        Status = status,
        PublishedAt = publishedAt ?? DateTimeOffset.UtcNow.AddDays(-1)
    };

    public void Dispose() => _context.Dispose();
}
