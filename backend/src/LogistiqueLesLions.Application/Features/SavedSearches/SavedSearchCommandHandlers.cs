using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.SavedSearches;

public class CreateSavedSearchCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateSavedSearchCommand, Result<Guid>>
{
    /// <summary>Tope por usuario, para que la lista siga siendo manejable.</summary>
    private const int MaxPerUser = 30;

    public async Task<Result<Guid>> Handle(CreateSavedSearchCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return Result<Guid>.Failure("SavedSearch.NameRequired");

        var count = await context.SavedSearches.CountAsync(s => s.UserId == request.UserId, ct);
        if (count >= MaxPerUser)
            return Result<Guid>.Failure("SavedSearch.LimitReached");

        var search = new SavedSearch
        {
            UserId       = request.UserId,
            Name         = name,
            FiltersJson  = SavedSearchFilters.Serialize(request.Filters),
            AlertEnabled = request.AlertEnabled,
            // Se parte de "ahora": los anuncios ya publicados no son novedad para quien
            // acaba de crear la búsqueda viéndolos en pantalla.
            LastNotifiedAt = DateTimeOffset.UtcNow
        };

        context.SavedSearches.Add(search);
        await context.SaveChangesAsync(ct);

        return Result<Guid>.Success(search.Id);
    }
}

public class UpdateSavedSearchCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateSavedSearchCommand, Result>
{
    public async Task<Result> Handle(UpdateSavedSearchCommand request, CancellationToken ct)
    {
        var search = await Find(context, request.UserId, request.SearchId, ct);
        if (search is null) return Result.Failure("SavedSearch.NotFound");

        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name)) return Result.Failure("SavedSearch.NameRequired");

        search.Name        = name;
        search.FiltersJson = SavedSearchFilters.Serialize(request.Filters);

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    internal static Task<SavedSearch?> Find(
        IApplicationDbContext context, Guid userId, Guid searchId, CancellationToken ct) =>
        // El filtro por UserId es la comprobación de propiedad: una búsqueda guardada
        // solo puede tocarla quien la creó.
        context.SavedSearches.FirstOrDefaultAsync(s => s.Id == searchId && s.UserId == userId, ct);
}

public class SetSavedSearchAlertCommandHandler(IApplicationDbContext context)
    : IRequestHandler<SetSavedSearchAlertCommand, Result>
{
    public async Task<Result> Handle(SetSavedSearchAlertCommand request, CancellationToken ct)
    {
        var search = await UpdateSavedSearchCommandHandler.Find(context, request.UserId, request.SearchId, ct);
        if (search is null) return Result.Failure("SavedSearch.NotFound");

        search.AlertEnabled = request.Enabled;

        // Al reactivar la alerta se parte de ahora: el usuario no quiere recibir de golpe
        // todo lo publicado mientras la tuvo apagada.
        if (request.Enabled) search.LastNotifiedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class DeleteSavedSearchCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteSavedSearchCommand, Result>
{
    public async Task<Result> Handle(DeleteSavedSearchCommand request, CancellationToken ct)
    {
        var search = await UpdateSavedSearchCommandHandler.Find(context, request.UserId, request.SearchId, ct);
        if (search is null) return Result.Failure("SavedSearch.NotFound");

        // Soft delete: el filtro global de consulta la oculta a partir de aquí.
        search.DeletedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
