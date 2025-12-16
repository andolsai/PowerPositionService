using System;

namespace PowerPositionService.Core.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateTime LondonNow { get; }

    DateTime ConvertToLondon(DateTime utcDateTime);
}
