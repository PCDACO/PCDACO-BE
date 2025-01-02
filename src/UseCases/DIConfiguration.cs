using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UseCases.Behaviors;
using UseCases.DTOs;

namespace UseCases;

public static class DIConfiguration
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<CurrentUser>();
        services.AddMediatR(option =>
        {
            option.RegisterServicesFromAssembly(typeof(DIConfiguration).Assembly);
            option.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}