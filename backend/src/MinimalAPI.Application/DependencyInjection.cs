using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MinimalAPI.Application.Abstractions.Messaging;
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

        // Integration event handlers — AUTO-SCAN toàn bộ assembly (giống AddValidatorsFromAssembly):
        // tạo handler mới là tự được đăng ký, không thể "quên AddScoped" rồi lặng lẽ mất message.
        // Scoped: mỗi message Kafka được xử lý trong một DI scope riêng (dùng được DbContext, repo...).
        // Mỗi handler được gán vào một consumer group trong appsettings (Kafka:Consumers) —
        // 1 topic có nhiều group cùng đọc độc lập, mỗi group chạy handler theo mục đích của mình.
        var handlerTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .Select(handlerInterface => (Service: handlerInterface, Implementation: type)));

        foreach (var (service, implementation) in handlerTypes)
            services.AddScoped(service, implementation);

        return services;
    }
}
