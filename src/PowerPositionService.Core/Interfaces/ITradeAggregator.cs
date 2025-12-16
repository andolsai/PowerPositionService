using System.Collections.Generic;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Core.Interfaces
{
    public interface ITradeAggregator
    {
        IEnumerable<AggregatedPowerPosition> AggregateTrades(IEnumerable<PowerTrade> trades);
    }
}
