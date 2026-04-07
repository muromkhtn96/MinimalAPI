using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MinimalAPI.Application.Behaviors;

namespace MinimalAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR + pipeline behaviors
        // Luồng: Request → Logging → Validation → Handler → Response
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // FluentValidation — tự scan validators từ assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
