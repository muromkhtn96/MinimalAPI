using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Abstractions.Messaging;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Infrastructure.Messaging.Kafka;
using MinimalAPI.Infrastructure.Persistence;
using MinimalAPI.Infrastructure.Persistence.Repositories;
using MinimalAPI.Infrastructure.Services;

namespace MinimalAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        // Đăng ký Redis Distributed Cache ở đây
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "MinimalAPI_";
        }); 

        // Đăng ký HyridCache (L1 + L2 wrapper)
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),     
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });
        
        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
        // services.AddTransient
        services.AddScoped<ICodeGenerator, CodeGenerator>();

        // Kafka — lifetime CHUẨN cho từng thành phần:
        //   - KafkaTopicResolver: Singleton — mapping event↔topic bất biến, build 1 lần, validate fail-fast
        //   - Producer: Singleton — IProducer thread-safe, giữ TCP connection, khởi tạo tốn kém
        //   - Consumer: AddHostedService — framework giữ đúng 1 instance chạy nền suốt vòng đời app
        //   - Handler (IIntegrationEventHandler<T>): Scoped, đăng ký ở Application/DependencyInjection.cs
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<KafkaTopicResolver>();
        services.AddSingleton<IIntegrationEventPublisher, KafkaProducerService>();
        services.AddHostedService<KafkaConsumerService>();

        return services;
    }
}