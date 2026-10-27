using Scalar.AspNetCore;

namespace Library.API.Configurations;

public static class OpenApiConfiguration
{
    public static IServiceCollection ConfigureOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options => options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new()
                {
                    Title = "Library API - Desafio Técnico",
                    Version = "v1",
                    Description = "API de Empréstimos Concorrentes e Auditáveis em .NET 10"
                };
                return Task.CompletedTask;
            }));

        return services;
    }

    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        app.MapOpenApi();

        app.MapScalarApiReference(options => options
            .WithTitle("Library API Documentation")
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));

        return app;
    }
}
