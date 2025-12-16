using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerPositionService.Core.Configuration;
using PowerPositionService.Core.Interfaces;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Core.Services;

public class CsvReportWriter : ICsvReportWriter
{
    private readonly ILogger<CsvReportWriter> _logger;
    private readonly PowerPositionSettings _settings;

    public CsvReportWriter(
        ILogger<CsvReportWriter> logger,
        IOptions<PowerPositionSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<string> WriteReportAsync(
        IEnumerable<AggregatedPowerPosition> positions, 
        DateTime extractDateTime)
    {
        var positionList = positions?.ToList() ?? new List<AggregatedPowerPosition>();
        
        if (!positionList.Any())
        {
            _logger.LogWarning("No positions to write to CSV");
            throw new InvalidOperationException("Cannot write empty report");
        }

        EnsureOutputDirectoryExists();

        var filename = GenerateFilename(extractDateTime);
        var filePath = Path.Combine(_settings.CsvOutputPath, filename);

        _logger.LogInformation("Writing power position report to {FilePath}", filePath);

        try
        {
            var csvContent = BuildCsvContent(positionList);
            
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                await writer.WriteAsync(csvContent);
            }

            _logger.LogInformation("Successfully wrote {PositionCount} positions to {FilePath}", 
                positionList.Count, filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write CSV report to {FilePath}", filePath);
            throw;
        }
    }

    private void EnsureOutputDirectoryExists()
    {
        if (string.IsNullOrWhiteSpace(_settings.CsvOutputPath))
        {
            throw new InvalidOperationException("CSV output path is not configured");
        }

        if (!Directory.Exists(_settings.CsvOutputPath))
        {
            _logger.LogInformation("Creating output directory: {Directory}", _settings.CsvOutputPath);
            Directory.CreateDirectory(_settings.CsvOutputPath);
        }
    }

    private static string GenerateFilename(DateTime extractDateTime)
    {
        return $"PowerPosition_{extractDateTime:yyyyMMdd}_{extractDateTime:HHmm}.csv";
    }

    private static string BuildCsvContent(IEnumerable<AggregatedPowerPosition> positions)
    {
        var sb = new StringBuilder();
        
        // Header row
        sb.AppendLine("Local Time,Volume");

        // Data rows
        foreach (var position in positions)
        {
            sb.AppendLine($"{position.FormattedLocalTime},{position.Volume}");
        }

        return sb.ToString();
    }
}
