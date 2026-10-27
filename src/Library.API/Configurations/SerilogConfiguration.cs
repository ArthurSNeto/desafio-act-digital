using Serilog;
using Serilog.Exceptions;

namespace Library.API.Configurations;

public static class SerilogConfiguration
{
    public static ConfigureHostBuilder ConfigureSerilog(this ConfigureHostBuilder builder)
    {
        builder.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("Application", "Library.API")
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

        return builder;
    }
}
