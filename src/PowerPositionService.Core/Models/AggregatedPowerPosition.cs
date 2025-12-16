using System;

namespace PowerPositionService.Core.Models;

public class AggregatedPowerPosition
{
    public int Hour { get; set; }

    public double Volume { get; set; }

    public string FormattedLocalTime => $"{Hour:D2}:00";
}
