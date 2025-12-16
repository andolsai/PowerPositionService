using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Services;
using ExternalPowerService = Services.PowerService;
using ExternalPowerTrade = Services.PowerTrade;

namespace PowerPositionService.Core.Services
{
    /// <summary>
    /// This wrapper implements our interface and delegates to the actual PowerService.
    /// </summary>
    public class PowerServiceAdapter : Interfaces.IPowerService
    {
        private readonly ILogger<PowerServiceAdapter> _logger;
        private readonly PowerService _powerService;

        public PowerServiceAdapter(ILogger<PowerServiceAdapter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _powerService = new ExternalPowerService();
        }

        public async Task<IEnumerable<Models.PowerTrade>> GetTradesAsync(DateTime date)
        {
            _logger.LogDebug("Fetching trades for date: {Date}", date);

            try
            {
                var trades = await _powerService.GetTradesAsync(date);
                
                var result = trades.Select(MapToInternalModel).ToList();
                
                _logger.LogDebug("Retrieved {Count} trades from PowerService", result.Count());
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trades for date {Date}", date);
                throw new InvalidOperationException($"Failed to fetch trades for {date:yyyy-MM-dd}", ex);
            }
        }

        private static Models.PowerTrade MapToInternalModel(ExternalPowerTrade externalTrade)
        {
            var periods = externalTrade.Periods
                .Select(p => new Models.PowerPeriod
                {
                    Period = p.Period,
                    Volume = p.Volume
                })
                .ToArray();

            return new Models.PowerTrade
            {
                Date = externalTrade.Date,
                Periods = periods
            };
        }

       
    }
}
