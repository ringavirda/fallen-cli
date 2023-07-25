// Vendor namespaces.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
// FCli namespaces.
using FCli;
using FCli.Services;
using FCli.Services.Data;
using FCli.Services.Format;

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
            .WriteTo.RollingFile(
                services.GetRequiredService<IConfig>().LogsPath,
                Serilog.Events.LogEventLevel.Information);
    })
    // Register app services according to their nature.
    .ConfigureServices(services => {
        services
            .AddSingleton<IConfig, DynamicConfig>()
            .AddSingleton<ICommandLineFormatter, InlineFormatter>()
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
                    serviceProvider.GetRequiredService<ICommandLineFormatter>(),
                    args
                )
            );
    })
    // Build and run fcli.
    .Build()
    .Run();
