using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Library.API.Configurations;

public static class HealthchecksConfiguration
{
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        string postgresConnection = configuration.GetConnectionString("PostgreSql")!;
        string redisConnection = configuration.GetConnectionString("Redis")!;

        services.AddHealthChecks()
            .AddNpgSql(postgresConnection, name: "postgresql", tags: ["ready"])
            .AddRedis(redisConnection, name: "redis", tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}
