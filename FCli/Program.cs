// Vendor namespaces.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
// FCli namespaces.
using FCli;
using FCli.Services;
using FCli.Services.Data;
using FCli.Services.Format;

// Configure application host.
var host = Host.CreateDefaultBuilder()
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
            .AddSingleton<FallenCli>();
    })
    // Build fcli host.
    .Build();

// Run main fallen-cli logic.
host.Services.GetRequiredService<FallenCli>().Execute(args);
