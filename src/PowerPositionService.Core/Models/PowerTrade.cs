using System;

namespace PowerPositionService.Core.Models
{
    /// <summary>
    /// Internal representation of a power trade.
    /// This isolates our code from the external Axpo.PowerTrade type.
    /// </summary>
    public class PowerTrade
    {
        /// <summary>
        /// The date of the trade.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The periods within this trade.
        /// </summary>
        public PowerPeriod[] Periods { get; set; } = Array.Empty<PowerPeriod>();
    }
}
