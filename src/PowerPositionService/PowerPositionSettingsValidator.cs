using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using PowerPositionService.Core.Configuration;

namespace PowerPositionService;

public class PowerPositionSettingsValidator : IValidateOptions<PowerPositionSettings>
{
    public ValidateOptionsResult Validate(string name, PowerPositionSettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.CsvOutputPath))
        {
            errors.Add("CsvOutputPath must be configured");
        }

        if (options.ExtractIntervalMinutes < 1)
        {
            errors.Add("ExtractIntervalMinutes must be at least 1 minute");
        }

        if (options.ExtractIntervalMinutes > 1440) // 24 hours
        {
            errors.Add("ExtractIntervalMinutes cannot exceed 1440 minutes (24 hours)");
        }

        if (options.MaxRetryAttempts < 1)
        {
            errors.Add("MaxRetryAttempts must be at least 1");
        }

        if (options.RetryDelaySeconds < 1)
        {
            errors.Add("RetryDelaySeconds must be at least 1");
        }

        return errors.Any() 
            ? ValidateOptionsResult.Fail(errors) 
            : ValidateOptionsResult.Success;
    }
}
