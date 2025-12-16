using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerPositionService.Core.Configuration;
using PowerPositionService.Core.Interfaces;

namespace PowerPositionService;

public class PowerPositionWorker : BackgroundService
{
    private readonly IPowerPositionExtractor _extractor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<PowerPositionWorker> _logger;
    private readonly PowerPositionSettings _settings;

    public PowerPositionWorker(
        IPowerPositionExtractor extractor,
        IDateTimeProvider dateTimeProvider,
        ILogger<PowerPositionWorker> logger,
        IOptions<PowerPositionSettings> settings)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Power Position Worker starting. Extract interval: {Interval} minutes",
            _settings.ExtractIntervalMinutes);

        try
        {
            _logger.LogInformation("Running initial extract on service start");
            await RunExtractWithLoggingAsync(stoppingToken);

            var interval = TimeSpan.FromMinutes(_settings.ExtractIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, stoppingToken);
                    
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await RunExtractWithLoggingAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Power Position Worker");
            throw;
        }

        _logger.LogInformation("Power Position Worker stopped");
    }

    private async Task RunExtractWithLoggingAsync(CancellationToken stoppingToken)
    {
        var startTime = _dateTimeProvider.LondonNow;
        _logger.LogInformation(
            "Starting scheduled extract at {StartTime} (London time)", 
            startTime);

        try
        {
            var success = await _extractor.ExecuteExtractAsync(stoppingToken);

            var endTime = _dateTimeProvider.LondonNow;
            var duration = endTime - startTime;

            if (success)
            {
                _logger.LogInformation(
                    "Extract completed successfully. Duration: {Duration}ms",
                    duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogError(
                    "Extract failed after all retry attempts. Duration: {Duration}ms. " +
                    "Next extract scheduled in {Interval} minutes.",
                    duration.TotalMilliseconds,
                    _settings.ExtractIntervalMinutes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during extract execution");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Power Position Worker is stopping...");
        await base.StopAsync(stoppingToken);
    }
}
