using Library.API.Services;
using Library.Application.Interfaces;
using Library.Domain.AuditModule;
using Library.Domain.BookModule;
using Library.Domain.LoanModule;
using Library.Domain.UnitOfWork;
using Library.Domain.UserModule;
using Library.Infra.EF.Repositories.AuditModule;
using Library.Infra.EF.Repositories.BookModule;
using Library.Infra.EF.Repositories.LoanModule;
using Library.Infra.EF.Repositories.UserModule;
using Library.Infra.EF.UnitOfWork;
using Library.Infra.Redis;

namespace Library.API.Configurations;

public static class DependencyInjectionConfigurations
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();

        services.AddSingleton<ICacheService, DistributedCacheService>();

        return services;
    }
}
