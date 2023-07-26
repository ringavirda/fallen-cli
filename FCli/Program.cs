// Vendor namespaces.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
// FCli namespaces.
using FCli;
using FCli.Services;
using FCli.Services.Data;
using System.Globalization;
using FCli.Services.Format;
using System.Resources;
using System.Reflection;
using FCli.Services.Config;

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
                    .First(format => format.Selector == "inline"));
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
        // Configure main app service.
        services
            .AddSingleton<IConfig>(config)
            .AddSingleton(new ResourceManager(
                "FCli.Resources.Strings",
                Assembly.GetExecutingAssembly()))
            .AddScoped<ICommandLoader, JsonLoader>()
            .AddScoped<ICommandFactory, OSSpecificFactory>()
            .AddScoped<IToolExecutor, ToolExecutor>()
            // Main entry point.
            .AddSingleton<FallenCli>();
    })
    // Build fcli host.
    .Build();


// Run main fallen-cli logic.
host.Services.GetRequiredService<FallenCli>().Execute(args);
