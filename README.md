# Power Position Service

A Windows Service that generates intra-day power position reports for power traders. The service aggregates trade volumes per hour and outputs CSV files at configurable intervals.

## Requirements

- .NET Framework 4.8
- Windows operating system
- PowerService.dll (external trading system interface)

## Prerequisites - PowerService.dll Setup

**Before building**, you must download the PowerService.dll from the trading system:

1. Go to: https://github.com/kkmoorthy/PetroineosCodingChallenge
2. Download `PowerService.dll`
3. Place it in the `lib/` directory of this solution

The DLL provides:
- `Axpo.PowerService` class with `GetTrades()` and `GetTradesAsync()` methods
- `Axpo.PowerTrade` class representing a trade with periods
- `Axpo.PowerPeriod` class representing a single period within a trade

## Project Structure

```
PowerPositionService/
├── lib/
│   └── PowerService.dll            # External DLL (download required)
├── src/
│   ├── PowerPositionService/           # Windows Service host application
│   │   ├── Program.cs                  # Entry point with DI configuration
│   │   ├── PowerPositionWorker.cs      # Background service worker
│   │   └── appsettings.json            # Configuration file
│   │
│   ├── PowerPositionService.Core/      # Core business logic library
│   │   ├── Configuration/
│   │   │   └── PowerPositionSettings.cs
│   │   ├── Interfaces/
│   │   │   ├── IPowerService.cs        # Uses Axpo.PowerTrade
│   │   │   ├── ITradeAggregator.cs     # Uses Axpo.PowerTrade
│   │   │   ├── ICsvReportWriter.cs
│   │   │   ├── IDateTimeProvider.cs
│   │   │   └── IPowerPositionExtractor.cs
│   │   ├── Models/
│   │   │   └── AggregatedPowerPosition.cs
│   │   └── Services/
│   │       ├── DateTimeProvider.cs
│   │       ├── TradeAggregator.cs
│   │       ├── CsvReportWriter.cs
│   │       ├── PowerServiceAdapter.cs  # Wraps Axpo.PowerService
│   │       └── PowerPositionExtractor.cs
│   │
│   └── PowerPositionService.Tests/     # Unit tests (NUnit + Moq)
│
└── PowerPositionService.sln            # Solution file
```

**Note:** This solution uses `Axpo.PowerTrade` and `Axpo.PowerPeriod` types directly from the external PowerService.dll rather than defining internal models. This avoids type conflicts and simplifies the architecture.

## SOLID Principles Applied

### Single Responsibility Principle (SRP)
- **TradeAggregator**: Only handles aggregation logic
- **CsvReportWriter**: Only handles CSV file generation
- **PowerServiceAdapter**: Only handles communication with external trading system
- **PowerPositionExtractor**: Only orchestrates the extraction workflow
- **DateTimeProvider**: Only provides date/time functionality

### Open/Closed Principle (OCP)
- The system is open for extension through interfaces but closed for modification
- New report writers or aggregation strategies can be added without modifying existing code

### Liskov Substitution Principle (LSP)
- All interfaces can be substituted with different implementations
- Mock implementations are used in unit tests

### Interface Segregation Principle (ISP)
- Small, focused interfaces (IPowerService, ITradeAggregator, ICsvReportWriter)
- No interface forces implementing unnecessary methods

### Dependency Inversion Principle (DIP)
- High-level modules depend on abstractions (interfaces)
- All dependencies are injected via constructor injection
- IDateTimeProvider allows for testable date/time handling

## Configuration

Edit `appsettings.json` to configure the service:

```json
{
  "PowerPositionSettings": {
    "CsvOutputPath": "C:\\PowerPositionReports",
    "ExtractIntervalMinutes": 60,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 10
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| CsvOutputPath | Directory where CSV reports are saved | Required |
| ExtractIntervalMinutes | Interval between extracts (1-1440 minutes) | 60 |
| MaxRetryAttempts | Number of retry attempts on failure | 3 |
| RetryDelaySeconds | Delay between retry attempts | 10 |

## Building

```bash
# Restore packages
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test

# Publish the service
dotnet publish src/PowerPositionService/PowerPositionService.csproj -c Release -r win-x64 --self-contained
```

## Installing as Windows Service

1. Build and publish the application
2. Open PowerShell as Administrator
3. Create the service:

```powershell
sc.exe create "PowerPositionService" binPath="C:\path\to\PowerPositionService.exe"
sc.exe description "PowerPositionService" "Generates intra-day power position reports"
sc.exe config "PowerPositionService" start=auto
```

4. Start the service:
```powershell
sc.exe start "PowerPositionService"
```

## Uninstalling

```powershell
sc.exe stop "PowerPositionService"
sc.exe delete "PowerPositionService"
```

## CSV Output Format

The service generates CSV files with the following format:

**Filename**: `PowerPosition_YYYYMMDD_HHMM.csv`

**Content**:
```csv
Local Time,Volume
23:00,150
00:00,150
01:00,150
...
22:00,80
```

- Local times are in London timezone (GMT/BST)
- The trading day starts at 23:00 the previous day
- Volume values are aggregated across all trades

## Period to Local Time Mapping

| Period | Local Time |
|--------|------------|
| 1 | 23:00 |
| 2 | 00:00 |
| 3 | 01:00 |
| ... | ... |
| 24 | 22:00 |

## Logging

The service uses Serilog for comprehensive logging:
- Console output (when running interactively)
- Rolling file logs in the `logs` directory
- Log levels: Debug, Information, Warning, Error, Critical

Log files are retained for 30 days and roll over daily.

### Log Location
Logs are stored in: `{ApplicationDirectory}/logs/PowerPositionService-YYYYMMDD.log`

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## PowerService.dll Integration

The `PowerServiceAdapter` class wraps the external `Axpo.PowerService` from PowerService.dll:

```csharp
public class PowerServiceAdapter : IPowerService
{
    private readonly Axpo.PowerService _powerService;
    
    public PowerServiceAdapter(ILogger<PowerServiceAdapter> logger)
    {
        _powerService = new Axpo.PowerService();
    }
    
    public async Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date)
    {
        var externalTrades = await _powerService.GetTradesAsync(date);
        return externalTrades.Select(MapToInternalModel);
    }
}
```

The adapter:
1. Creates an instance of `Axpo.PowerService` from the external DLL
2. Calls `GetTradesAsync()` to fetch trades for a given date
3. Maps `Axpo.PowerTrade` and `Axpo.PowerPeriod` to internal models
4. Handles exceptions and logging

## Error Handling

- The service implements retry logic for transient failures
- Failed extracts are logged but don't stop the service
- The service continues running and attempts the next scheduled extract

## Troubleshooting

### Service won't start
- Check Windows Event Log for errors
- Verify appsettings.json configuration is valid
- Ensure CsvOutputPath directory is accessible

### Missing CSV files
- Check log files for errors during extraction
- Verify PowerService.dll is accessible
- Check disk space in output directory

### Incorrect times in CSV
- The service uses London local time (GMT/BST)
- Verify system timezone settings

## License

This project is proprietary software developed for Petroineos.
