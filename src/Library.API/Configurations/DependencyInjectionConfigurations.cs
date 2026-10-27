using Library.Domain.Audit;
using Library.Domain.UnitOfWork;
using Library.Infra.EF.Repositories.Audit;
using Library.Infra.EF.UnitOfWork;

namespace Library.API.Configurations;

public static class DependencyInjectionConfigurations
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();

        return services;
    }
}
