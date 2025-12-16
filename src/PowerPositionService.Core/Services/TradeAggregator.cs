using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PowerPositionService.Core.Interfaces;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Core.Services
{
    public class TradeAggregator : ITradeAggregator
    {
        private readonly ILogger<TradeAggregator> _logger;

        public TradeAggregator(ILogger<TradeAggregator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<AggregatedPowerPosition> AggregateTrades(IEnumerable<PowerTrade> trades)
        {
            var tradeList = trades?.ToList() ?? new List<PowerTrade>();
            
            if (!tradeList.Any())
            {
                _logger.LogWarning("No trades provided for aggregation");
                return Enumerable.Empty<AggregatedPowerPosition>();
            }

            _logger.LogDebug("Aggregating {TradeCount} trades", tradeList.Count);

            var volumeByPeriod = new Dictionary<int, double>();

            foreach (var trade in tradeList)
            {
                if (trade.Periods == null)
                {
                    _logger.LogWarning("Trade for date {Date} has null periods", trade.Date);
                    continue;
                }

                foreach (var period in trade.Periods)
                {
                    if (period.Period < 1 || period.Period > 24)
                    {
                        _logger.LogWarning("Invalid period number {Period} in trade for date {Date}", 
                            period.Period, trade.Date);
                        continue;
                    }

                    if (!volumeByPeriod.ContainsKey(period.Period))
                    {
                        volumeByPeriod[period.Period] = 0;
                    }

                    volumeByPeriod[period.Period] += period.Volume;
                }
            }

            var positions = volumeByPeriod
                .Select(kvp => new AggregatedPowerPosition
                {
                    Hour = MapPeriodToHour(kvp.Key),
                    Volume = kvp.Value
                })
                .OrderBy(p => GetSortOrder(p.Hour))
                .ToList();

            _logger.LogDebug("Aggregation complete. Generated {PositionCount} hourly positions", positions.Count);

            return positions;
        }

        private static int MapPeriodToHour(int period)
        {
            return (22 + period) % 24;
        }

        private static int GetSortOrder(int hour)
        {
            return hour >= 23 ? hour - 23 : hour + 1;
        }
    }
}
