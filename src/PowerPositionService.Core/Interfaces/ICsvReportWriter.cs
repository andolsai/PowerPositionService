using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Core.Interfaces;

/// <summary>
/// Interface for writing aggregated positions to CSV files.
/// Single Responsibility: CSV file generation only.
/// </summary>
public interface ICsvReportWriter
{
    /// <summary>
    /// Writes the aggregated positions to a CSV file.
    /// </summary>
    /// <param name="positions">The positions to write.</param>
    /// <param name="extractDateTime">The date/time of the extract (for filename).</param>
    /// <returns>The path to the generated CSV file.</returns>
    Task<string> WriteReportAsync(IEnumerable<AggregatedPowerPosition> positions, DateTime extractDateTime);
}
