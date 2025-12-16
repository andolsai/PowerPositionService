namespace PowerPositionService.Core.Configuration;

/// <summary>
/// Configuration settings for the Power Position Service.
/// </summary>
public class PowerPositionSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "PowerPositionSettings";

    /// <summary>
    /// The directory path where CSV files will be saved.
    /// </summary>
    public string CsvOutputPath { get; set; } = string.Empty;

    /// <summary>
    /// The interval in minutes between extracts.
    /// </summary>
    public int ExtractIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum number of retry attempts for failed extracts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay in seconds between retry attempts.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 10;
}
