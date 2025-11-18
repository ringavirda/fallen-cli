using System.Globalization;
using System.Reflection;
using System.Text;

using FCli;
using FCli.Services;
using FCli.Services.Abstractions;
using FCli.Services.Config;
using FCli.Services.Data;
using FCli.Services.Data.Identity;
using FCli.Services.Encryption;
using FCli.Services.Tools;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

// Configure application host.
var host = Host.CreateDefaultBuilder()
    // Register app services according to their nature.
    .ConfigureServices(services =>
    {
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
        // Set console encoding to unicode.
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        // Check if need to use encryption for the user data.
        if (config.UseEncryption)
            services.AddSingleton<IIdentityManager, EncryptedIdentityManager>();
        else
            services.AddSingleton<IIdentityManager, PlainIdentityManager>();
        // Configure app services.
        services
            .AddSingleton<IConfig>(config)
            .AddSingleton<IResources, StringResources>()
            .AddSingleton<IArgsParser, ArgsParser>()
            .AddScoped<ICommandLoader, JsonLoader>()
            .AddScoped<IEncryptor, AesEncryptor>()
            .AddScoped<IMailer, CombinedMailer>()
            .AddScoped<ICommandFactory, SystemSpecificFactory>()
            .AddScoped<IToolExecutor, ToolExecutor>()
            // Main entry point.
            .AddSingleton<FallenCli>();
        // Add all the tools.
        var toolTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass
                        && t.IsPublic
                        && t.IsSubclassOf(typeof(ToolBase)));
        foreach (var toolType in toolTypes)
            services.AddScoped(typeof(ITool), toolType);
    })
    // Serilog for structured file logging.
    .UseSerilog((context, services, configuration)
        => configuration
            // Some enrichers for more info in log files.
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            // Rolling file for more organization.
            .WriteTo.RollingFile(
                services.GetRequiredService<IConfig>().LogsPath,
                Serilog.Events.LogEventLevel.Information,
                formatProvider: CultureInfo.InvariantCulture))
    // Build fcli host.
    .Build();

// Run main fallen-cli logic.
host.Services.GetRequiredService<FallenCli>().Execute(args);