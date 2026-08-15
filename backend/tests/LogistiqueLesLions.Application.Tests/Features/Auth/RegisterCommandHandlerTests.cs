using FluentAssertions;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Features.Auth.Commands.Register;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogistiqueLesLions.Application.Tests.Features.Auth;

public class RegisterCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var auditInterceptor = new Infrastructure.Persistence.Interceptors.AuditInterceptor(mockCurrentUser.Object);
        var auditLogInterceptor = new Infrastructure.Persistence.Interceptors.AuditLogInterceptor(
            mockCurrentUser.Object,
            new Microsoft.AspNetCore.Http.HttpContextAccessor());

        _context = new ApplicationDbContext(options, auditInterceptor, auditLogInterceptor);

        var mockJwt = new Mock<IJwtService>();
        mockJwt.Setup(j => j.GenerateAccessToken(It.IsAny<UserProfile>())).Returns("access-token");
        mockJwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");

        _handler = new RegisterCommandHandler(_context, mockJwt.Object);
    }

    private static RegisterCommand Command(string phone = "77 123 45 67", string? email = null) =>
        new(phone, "motdepasse123", "Mamadou Diop", AccountType.Particulier, "DK", "Dakar", email);

    [Fact]
    public async Task Handle_DeberiaGuardarElTelefonoNormalizado()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var user = await _context.UserProfiles.SingleAsync();
        user.Phone.Should().Be("+221771234567");
        user.DisplayName.Should().Be("Mamadou Diop");
        user.Region.Should().Be("DK");
        user.City.Should().Be("Dakar");
    }

    [Fact]
    public async Task Handle_DeberiaCrearSiempreCuentasConRolUser()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        var user = await _context.UserProfiles.SingleAsync();
        user.Role.Should().Be(UserRole.User);
        user.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public async Task Handle_DeberiaRechazarUnTelefonoYaRegistrado_AunqueSeEscribaDeOtraForma()
    {
        await _handler.Handle(Command("77 123 45 67"), CancellationToken.None);

        var result = await _handler.Handle(Command("+221771234567"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Auth.PhoneAlreadyExists");
        (await _context.UserProfiles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeberiaRechazarUnTelefonoNoSenegales()
    {
        var result = await _handler.Handle(Command("+34612345678"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Auth.InvalidPhone");
    }

    [Fact]
    public async Task Handle_DeberiaAceptarUnaCuentaSinCorreo()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.UserProfiles.SingleAsync()).Email.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeberiaRechazarUnCorreoYaRegistrado()
    {
        await _handler.Handle(Command("77 123 45 67", "AWA@example.com"), CancellationToken.None);

        var result = await _handler.Handle(Command("78 999 88 77", "awa@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Auth.EmailAlreadyExists");
    }

    public void Dispose() => _context.Dispose();
}
