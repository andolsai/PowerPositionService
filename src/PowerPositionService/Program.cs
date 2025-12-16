using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PowerPositionService;
using PowerPositionService.Core.Configuration;
using PowerPositionService.Core.Interfaces;
using PowerPositionService.Core.Services;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "PowerPositionService-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Power Position Service");

    var builder = Host.CreateDefaultBuilder(args);
    
    builder.UseWindowsService(options =>
    {
        options.ServiceName = "Power Position Service";
    });

    builder.UseSerilog();

    builder.ConfigureServices((hostContext, services) =>
    {
        services.Configure<PowerPositionSettings>(
            hostContext.Configuration.GetSection(PowerPositionSettings.SectionName));

        services.AddSingleton<IValidateOptions<PowerPositionSettings>, PowerPositionSettingsValidator>();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPowerService, PowerServiceAdapter>();
        services.AddSingleton<ITradeAggregator, TradeAggregator>();
        services.AddSingleton<ICsvReportWriter, CsvReportWriter>();
        services.AddSingleton<IPowerPositionExtractor, PowerPositionExtractor>();

        services.AddHostedService<PowerPositionWorker>();
    });

    var host = builder.Build();

    var settingsOptions = host.Services.GetRequiredService<IOptions<PowerPositionSettings>>();
    var settings = settingsOptions.Value;
    
    Log.Information("Configuration loaded - Extract interval: {Interval} minutes, Output path: {Path}",
        settings.ExtractIntervalMinutes, settings.CsvOutputPath);

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
