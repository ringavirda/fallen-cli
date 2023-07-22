using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

using FCli;
using FCli.Services;
using FCli.Services.Data;

const string configurationFolder = ".config";

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((context, builder) => builder
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(Path.Combine(configurationFolder, "appsettings.json"),
            optional: true, reloadOnChange: false)
    )
    .UseSerilog((context, services, configuration) =>
        configuration
            .MinimumLevel.Error()
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(context.Configuration)
            .WriteTo.File(Path.Combine(
                services.GetRequiredService<DynamicConfig>().StorageLocation,
                context.Configuration.GetSection("Storage")
                    .GetSection("LogFolderName").Value ?? "Logs",
                    "Log"),
                rollingInterval: RollingInterval.Day)
    )
    .ConfigureServices(services =>
        services.AddSingleton<DynamicConfig>()
            .AddScoped<ICommandLoader, JsonLoader>()
            .AddSingleton<CommandFactory>()
            .AddSingleton<ToolExecutor>()
            .AddSingleton<FallenCli>()
    )
    .Build();

var fcli = host.Services.GetRequiredService<FallenCli>();
fcli.Run(args);