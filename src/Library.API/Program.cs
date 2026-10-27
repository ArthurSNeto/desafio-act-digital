using Library.API.Configurations;
using Serilog;
using System.Diagnostics;

namespace Library.API;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Host.ConfigureSerilog();

            builder.Services.ConfigureOpenApi();

            builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddInfrastructureData(builder.Configuration);

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "Library:";
            });

            builder.Services.ConfigureHealthChecks(builder.Configuration);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            WebApplication app = builder.Build();

            app.UseSerilogRequestLogging();
            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.MapApi();
            }

            app.MapHealthChecks();

            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "A aplicação falhou ao iniciar.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
