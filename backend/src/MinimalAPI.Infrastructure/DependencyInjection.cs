using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Interfaces;
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
            options.Configuration = "localhost:6379";
            options.InstanceName = "MinimalAPI_";
        }); 

        // Đăng ký HyridCache
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
        services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();

        // Helper sinh mã code tuần tự theo prefix
        services.AddScoped<ICodeGenerator, CodeGenerator>();

        return services;
    }
}