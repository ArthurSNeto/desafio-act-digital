using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.OpenTelemetry;

namespace Library.API.Configurations;

public static class SerilogConfiguration
{
    public static ConfigureHostBuilder ConfigureSerilog(this ConfigureHostBuilder builder)
    {
        builder.UseSerilog((context, services, configuration) =>
        {
            string? otlpEndpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            string applicationName = context.HostingEnvironment.ApplicationName;

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Application", applicationName)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                configuration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = applicationName
                    };
                });
            }
        });

        return builder;
    }
}
