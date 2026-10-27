using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.UnitOfWork;
using Library.Infra.EF.Repositories.AuditModule;
using Library.Infra.EF.Repositories.BookModule;
using Library.Infra.EF.UnitOfWork;

namespace Library.API.Configurations;

public static class DependencyInjectionConfigurations
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IBookRepository, BookRepository>();

        return services;
    }
}
