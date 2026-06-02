using Microsoft.EntityFrameworkCore;
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

        // EF Core — KHÔNG bật EnableRetryOnFailure để dùng transaction
        // tường minh (BeginTransaction/Commit/Rollback) theo kiểu try/catch.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // IApplicationDbContext — query side dùng LINQ (AsNoTracking)
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();

        // Helper sinh mã code tuần tự theo prefix
        services.AddScoped<ICodeGenerator, CodeGenerator>();

        return services;
    }
}
