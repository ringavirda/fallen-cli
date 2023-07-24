// Vendor namespaces.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;
// FCli namespaces.
using FCli;
using FCli.Services;
using FCli.Services.Data;

// Configure and run application host.
Host.CreateDefaultBuilder()
    // Serilog for structured file logging.
    .UseSerilog((context, services, configuration) =>
    {
        configuration
            // Some enrichers for more info in log files.
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            // Rolling file for more organization.
            // Json log format.
            .WriteTo.RollingFile(
                new JsonFormatter(),
                services.GetRequiredService<DynamicConfig>().LogsPath,
                Serilog.Events.LogEventLevel.Debug);
    })
    // Register app services according to their nature.
    .ConfigureServices(services => {
        services
            .AddSingleton<DynamicConfig>()
            .AddScoped<ICommandLoader, JsonLoader>()
            .AddScoped<ICommandFactory, OSSpecificFactory>()
            .AddScoped<IToolExecutor, GenericExecutor>()
            // Main service configuration.
            .AddHostedService(
                serviceProvider => new FallenCli(
                    serviceProvider.GetRequiredService<IToolExecutor>(),
                    serviceProvider.GetRequiredService<ICommandFactory>(),
                    serviceProvider.GetRequiredService<ILogger<FallenCli>>(),
                    serviceProvider.GetRequiredService<IHost>(),
                    args
                )
            );
    })
    // Build and run fcli.
    .Build()
    .Run();
