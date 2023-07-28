// Vendor namespaces.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using Serilog;
// FCli namespaces.
using FCli;
using FCli.Services;
using FCli.Services.Abstractions;
using FCli.Services.Config;
using FCli.Services.Data;

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
        // Load user's dynamic config.
        var config = new DynamicConfig();
        // Set user's preferred formatter.
        if (config.KnownFormatters.Contains(config.Formatter))
            services.AddSingleton(
                typeof(ICommandLineFormatter), config.Formatter.Type);
        // Guard against unknown formatter.
        // Use default if so.
        else
        {
            Console.WriteLine(
                "Warn! Config contains unknown formatter - using default instead.");
            services.AddSingleton(
                typeof(ICommandLineFormatter), config.KnownFormatters
                    .First(format => format.Selector == "inline").Type);
        }
        // Set user's preferred locale.
        if (config.KnownLocales.Contains(config.Locale))
            CultureInfo.CurrentUICulture 
                = CultureInfo.CreateSpecificCulture(config.Locale);
        // Guard against unknown locale.
        // Use default if so.
        else
        {
            Console.WriteLine(
                "Warn! Config contains unknown locale - using default instead.");
            CultureInfo.CurrentUICulture = 
                CultureInfo.CreateSpecificCulture("en");
        }
        // Configure app services.
        services
            .AddSingleton<IConfig>(config)
            .AddSingleton<IResources, StringResources>()
            .AddSingleton<IArgsParser, ArgsParser>()
            .AddScoped<ICommandLoader, JsonLoader>()
            .AddScoped<ICommandFactory, SystemSpecificFactory>()
            .AddScoped<IToolExecutor, ToolExecutor>()
            // Main entry point.
            .AddSingleton<FallenCli>();
    })
    // Build fcli host.
    .Build();


// Run main fallen-cli logic.
host.Services.GetRequiredService<FallenCli>().Execute(args);
