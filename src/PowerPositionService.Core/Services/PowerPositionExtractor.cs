using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerPositionService.Core.Configuration;
using PowerPositionService.Core.Interfaces;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Core.Services;

public class PowerPositionExtractor : IPowerPositionExtractor
{
    private readonly IPowerService _powerService;
    private readonly ITradeAggregator _tradeAggregator;
    private readonly ICsvReportWriter _csvReportWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<PowerPositionExtractor> _logger;
    private readonly PowerPositionSettings _settings;

    public PowerPositionExtractor(
        IPowerService powerService,
        ITradeAggregator tradeAggregator,
        ICsvReportWriter csvReportWriter,
        IDateTimeProvider dateTimeProvider,
        ILogger<PowerPositionExtractor> logger,
        IOptions<PowerPositionSettings> settings)
    {
        _powerService = powerService ?? throw new ArgumentNullException(nameof(powerService));
        _tradeAggregator = tradeAggregator ?? throw new ArgumentNullException(nameof(tradeAggregator));
        _csvReportWriter = csvReportWriter ?? throw new ArgumentNullException(nameof(csvReportWriter));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<bool> ExecuteExtractAsync(CancellationToken cancellationToken = default)
    {
        var extractStartTime = _dateTimeProvider.LondonNow;
        _logger.LogInformation("Starting power position extract at {ExtractTime} (London time)", 
            extractStartTime);

        var attempts = 0;
        var maxAttempts = _settings.MaxRetryAttempts;

        while (attempts < maxAttempts)
        {
            attempts++;
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Extract cancelled");
                return false;
            }

            try
            {
                await ExecuteExtractInternalAsync(extractStartTime, cancellationToken);
                _logger.LogInformation("Power position extract completed successfully on attempt {Attempt}", 
                    attempts);
                return true;
            }
            catch (Exception ex) when (attempts < maxAttempts)
            {
                _logger.LogWarning(ex, 
                    "Extract attempt {Attempt} of {MaxAttempts} failed. Retrying in {Delay} seconds...",
                    attempts, maxAttempts, _settings.RetryDelaySeconds);
                
                await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Extract failed after {MaxAttempts} attempts", maxAttempts);
                return false;
            }
        }

        return false;
    }

    private async Task ExecuteExtractInternalAsync(DateTime extractTime, CancellationToken cancellationToken)
    {
        List<PowerTrade> tradeList = await GetLocalTradesForToday(extractTime);

        List<AggregatedPowerPosition> positionList = AggragateTradesByHour(tradeList);

        await WriteCsvReport(extractTime, positionList);
    }

    private async Task WriteCsvReport(DateTime extractTime, List<AggregatedPowerPosition> positionList)
    {
        _logger.LogDebug("Writing CSV report");
        var filePath = await _csvReportWriter.WriteReportAsync(positionList, extractTime);

        _logger.LogInformation("Extract complete. Report saved to: {FilePath}", filePath);
    }

    private List<AggregatedPowerPosition> AggragateTradesByHour(List<PowerTrade> tradeList)
    {
        _logger.LogDebug("Aggregating trades");
        var aggregatedPositions = _tradeAggregator.AggregateTrades(tradeList);
        var positionList = aggregatedPositions.ToList();

        _logger.LogDebug("Aggregated into {PositionCount} hourly positions", positionList.Count);
        return positionList;
    }

    private async Task<List<PowerTrade>> GetLocalTradesForToday(DateTime extractTime)
    {
        var tradeDate = extractTime.Date;
        _logger.LogDebug("Fetching trades for date: {TradeDate}", tradeDate);

        var trades = await _powerService.GetTradesAsync(tradeDate);
        var tradeList = trades.ToList();

        _logger.LogDebug("Retrieved {TradeCount} trades", tradeList.Count);

        if (!tradeList.Any())
        {
            _logger.LogWarning("No trades returned for date {TradeDate}", tradeDate);
        }

        return tradeList;
    }
}
