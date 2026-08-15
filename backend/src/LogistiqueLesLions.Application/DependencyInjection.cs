using System.Reflection;
using FluentValidation;
using LogistiqueLesLions.Application.Common.Behaviors;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LogistiqueLesLions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IPriceIndicatorService, PriceIndicatorService>();
        services.AddScoped<IPriceDropAlertService, PriceDropAlertService>();
        services.AddScoped<INewVehicleAlertService, NewVehicleAlertService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IVehicleValuationService, VehicleValuationService>();

        return services;
    }
}
