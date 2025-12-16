# External Dependencies

This directory should contain the following external DLL:

## PowerService.dll

Download from: https://github.com/kkmoorthy/PetroineosCodingChallenge

This DLL provides the interface to the trading system and contains:
- `Axpo.PowerService` class with `GetTrades(DateTime date)` and `GetTradesAsync(DateTime date)` methods
- `Axpo.PowerTrade` class representing a trade with periods
  - `Date` property (DateTime)
  - `Periods` property (PowerPeriod[])
  - `Create(DateTime date, int numberOfPeriods)` static factory method
- `Axpo.PowerPeriod` class representing a single period within a trade
  - `Period` property (int) - Period number 1-24
  - `Volume` property (double) - Volume for this period

### Setup Instructions

1. Clone or download from the GitHub repository above
2. Copy `PowerService.dll` to this `lib` directory
3. Build the solution

The project files reference this location:
```xml
<Reference Include="PowerService">
  <HintPath>..\..\..\lib\PowerService.dll</HintPath>
</Reference>
```

### Period Mapping

The PowerService uses the following period-to-time mapping:
- Period 1 = 23:00 (previous day)
- Period 2 = 00:00
- Period 3 = 01:00
- ...
- Period 24 = 22:00
