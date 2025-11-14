using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneticAnalyzers.SQLDBFirst.Domain.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Domain.Services;
using PhoneticAnalyzers.SQLDBFirst.Infrastructure.Persistence;
using PhoneticAnalyzers.SQLDBFirst.Infrastructure.Repositories;
using PhoneticAnalyzers.SQLDBFirst.Infrastructure.Services;

namespace PhoneticAnalyzers.SQLDBFirst.Infrastructure;

/// <summary>
/// Dependency injection extensions for Infrastructure layer.
/// Registers DbContext, repositories, and services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSQLDBFirstInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext with PostgreSQL
        services.AddDbContext<PhoneticDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register repositories
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IPersonSearchRepository, PersonSearchRepository>();
        services.AddScoped<INicknameMapRepository, NicknameMapRepository>();

        // Register services
        services.AddSingleton<IPhoneticEncodingService, PhoneticEncodingService>();
        services.AddScoped<INicknameExpansionService, NicknameExpansionService>();

        return services;
    }
}
