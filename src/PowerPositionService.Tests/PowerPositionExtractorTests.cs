using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PowerPositionService.Core.Configuration;
using PowerPositionService.Core.Interfaces;
using PowerPositionService.Core.Models;
using PowerPositionService.Core.Services;

namespace PowerPositionService.Tests
{
    [TestFixture]
    public class PowerPositionExtractorTests
    {
        private Mock<IPowerService> _powerServiceMock;
        private Mock<ITradeAggregator> _tradeAggregatorMock;
        private Mock<ICsvReportWriter> _csvReportWriterMock;
        private Mock<IDateTimeProvider> _dateTimeProviderMock;
        private Mock<ILogger<PowerPositionExtractor>> _loggerMock;
        private Mock<IOptions<PowerPositionSettings>> _settingsMock;
        private PowerPositionSettings _settings;
        private PowerPositionExtractor _extractor;

        [SetUp]
        public void Setup()
        {
            _powerServiceMock = new Mock<IPowerService>();
            _tradeAggregatorMock = new Mock<ITradeAggregator>();
            _csvReportWriterMock = new Mock<ICsvReportWriter>();
            _dateTimeProviderMock = new Mock<IDateTimeProvider>();
            _loggerMock = new Mock<ILogger<PowerPositionExtractor>>();
            _settingsMock = new Mock<IOptions<PowerPositionSettings>>();

            _settings = new PowerPositionSettings
            {
                CsvOutputPath = "/tmp/test",
                ExtractIntervalMinutes = 60,
                MaxRetryAttempts = 3,
                RetryDelaySeconds = 1
            };

            _settingsMock.Setup(x => x.Value).Returns(_settings);

            var now = DateTime.UtcNow;
            _dateTimeProviderMock.Setup(x => x.LondonNow).Returns(now);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

            _extractor = new PowerPositionExtractor(
                _powerServiceMock.Object,
                _tradeAggregatorMock.Object,
                _csvReportWriterMock.Object,
                _dateTimeProviderMock.Object,
                _loggerMock.Object,
                _settingsMock.Object);
        }

        [Test]
        public async Task ExecuteExtractAsync_WhenSuccessful_ReturnsTrue()
        {
            SetupSuccessfulExtract();
            var result = await _extractor.ExecuteExtractAsync();
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ExecuteExtractAsync_CallsServicesInCorrectOrder()
        {
            var callOrder = new List<string>();
            
            _powerServiceMock
                .Setup(x => x.GetTradesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<PowerTrade> { CreateTestTrade() })
                .Callback(() => callOrder.Add("PowerService"));

            _tradeAggregatorMock
                .Setup(x => x.AggregateTrades(It.IsAny<IEnumerable<PowerTrade>>()))
                .Returns(CreateTestPositions())
                .Callback(() => callOrder.Add("TradeAggregator"));

            _csvReportWriterMock
                .Setup(x => x.WriteReportAsync(It.IsAny<IEnumerable<AggregatedPowerPosition>>(), It.IsAny<DateTime>()))
                .ReturnsAsync("/tmp/test.csv")
                .Callback(() => callOrder.Add("CsvReportWriter"));

            await _extractor.ExecuteExtractAsync();

            Assert.That(callOrder, Is.EqualTo(new[] { "PowerService", "TradeAggregator", "CsvReportWriter" }));
        }

        [Test]
        public async Task ExecuteExtractAsync_UsesLondonDateForTradeRetrieval()
        {
            var londonDate = new DateTime(2024, 6, 15, 10, 30, 0);
            _dateTimeProviderMock.Setup(x => x.LondonNow).Returns(londonDate);
            SetupSuccessfulExtract();

            await _extractor.ExecuteExtractAsync();

            _powerServiceMock.Verify(x => x.GetTradesAsync(londonDate.Date), Times.Once);
        }

        [Test]
        public async Task ExecuteExtractAsync_WhenPowerServiceFails_RetriesUpToMaxAttempts()
        {
            _settings.MaxRetryAttempts = 3;
            _settings.RetryDelaySeconds = 0;
            
            _powerServiceMock
                .Setup(x => x.GetTradesAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Service error"));

            var result = await _extractor.ExecuteExtractAsync();

            Assert.That(result, Is.False);
            _powerServiceMock.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>()), Times.Exactly(3));
        }

        [Test]
        public async Task ExecuteExtractAsync_WhenCancelled_ReturnsFalse()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _extractor.ExecuteExtractAsync(cts.Token);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Constructor_WithNullPowerService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PowerPositionExtractor(
                null,
                _tradeAggregatorMock.Object,
                _csvReportWriterMock.Object,
                _dateTimeProviderMock.Object,
                _loggerMock.Object,
                _settingsMock.Object));
        }

        private void SetupSuccessfulExtract()
        {
            _powerServiceMock
                .Setup(x => x.GetTradesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new[] { CreateTestTrade() });

            _tradeAggregatorMock
                .Setup(x => x.AggregateTrades(It.IsAny<IEnumerable<PowerTrade>>()))
                .Returns(CreateTestPositions());

            _csvReportWriterMock
                .Setup(x => x.WriteReportAsync(It.IsAny<IEnumerable<AggregatedPowerPosition>>(), It.IsAny<DateTime>()))
                .ReturnsAsync("/tmp/test.csv");
        }

        private static PowerTrade CreateTestTrade()
        {
            var periods = new PowerPeriod[24];
            for (int i = 0; i < 24; i++)
            {
                periods[i] = new PowerPeriod { Period = i + 1, Volume = 100 };
            }
            return new PowerTrade { Date = DateTime.Today, Periods = periods };
        }

        private static IEnumerable<AggregatedPowerPosition> CreateTestPositions()
        {
            return new List<AggregatedPowerPosition>
            {
                new AggregatedPowerPosition { Hour = 23, Volume = 100 },
                new AggregatedPowerPosition { Hour = 0, Volume = 100 }
            };
        }
    }
}
