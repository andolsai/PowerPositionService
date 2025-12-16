using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PowerPositionService.Core.Configuration;
using PowerPositionService.Core.Models;
using PowerPositionService.Core.Services;

namespace PowerPositionService.Tests
{
    [TestFixture]
    public class CsvReportWriterTests
    {
        private Mock<ILogger<CsvReportWriter>> _loggerMock = null!;
        private Mock<IOptions<PowerPositionSettings>> _settingsMock = null!;
        private PowerPositionSettings _settings = null!;
        private CsvReportWriter _writer = null!;
        private string _testOutputPath = null!;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<CsvReportWriter>>();
            _settingsMock = new Mock<IOptions<PowerPositionSettings>>();
            
            _testOutputPath = Path.Combine(Path.GetTempPath(), string.Format("PowerPositionTests_{0}", Guid.NewGuid()));
            Directory.CreateDirectory(_testOutputPath);
            
            _settings = new PowerPositionSettings
            {
                CsvOutputPath = _testOutputPath,
                ExtractIntervalMinutes = 60
            };
            
            _settingsMock.Setup(x => x.Value).Returns(_settings);
            _writer = new CsvReportWriter(_loggerMock.Object, _settingsMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }

        [Test]
        public async Task WriteReportAsync_CreatesFileWithCorrectName()
        {
            var positions = CreateTestPositions();
            var extractTime = new DateTime(2014, 12, 20, 18, 37, 0);

            var filePath = await _writer.WriteReportAsync(positions, extractTime);

            Assert.That(File.Exists(filePath), Is.True);
            Assert.That(Path.GetFileName(filePath), Is.EqualTo("PowerPosition_20141220_1837.csv"));
        }

        [Test]
        public async Task WriteReportAsync_CreatesFileWithCorrectHeader()
        {
            var positions = CreateTestPositions();
            var extractTime = DateTime.Now;

            var filePath = await _writer.WriteReportAsync(positions, extractTime);
            var lines = File.ReadAllLines(filePath);

            Assert.That(lines[0], Is.EqualTo("Local Time,Volume"));
        }

        [Test]
        public async Task WriteReportAsync_WritesCorrectData()
        {
            var positions = new List<AggregatedPowerPosition>
            {
                new AggregatedPowerPosition { Hour = 23, Volume = 150 },
                new AggregatedPowerPosition { Hour = 0, Volume = 100 },
                new AggregatedPowerPosition { Hour = 13, Volume = -50 }
            };
            var extractTime = DateTime.Now;

            var filePath = await _writer.WriteReportAsync(positions, extractTime);
            var lines = File.ReadAllLines(filePath);

            Assert.That(lines, Has.Length.EqualTo(4));
            Assert.That(lines[1], Is.EqualTo("23:00,150"));
            Assert.That(lines[2], Is.EqualTo("00:00,100"));
            Assert.That(lines[3], Is.EqualTo("13:00,-50"));
        }

        [Test]
        public async Task WriteReportAsync_FormatsTimeAs24Hour()
        {
            var positions = new List<AggregatedPowerPosition>
            {
                new AggregatedPowerPosition { Hour = 1, Volume = 100 },
                new AggregatedPowerPosition { Hour = 13, Volume = 200 }
            };
            var extractTime = DateTime.Now;

            var filePath = await _writer.WriteReportAsync(positions, extractTime);
            var lines = File.ReadAllLines(filePath);

            Assert.That(lines[1], Does.StartWith("01:00"));
            Assert.That(lines[2], Does.StartWith("13:00"));
        }

        [Test]
        public void WriteReportAsync_WithEmptyPositions_ThrowsException()
        {
            var positions = Enumerable.Empty<AggregatedPowerPosition>();
            var extractTime = DateTime.Now;

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _writer.WriteReportAsync(positions, extractTime));
        }

        [Test]
        public void WriteReportAsync_WithNullPositions_ThrowsException()
        {
            var extractTime = DateTime.Now;

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _writer.WriteReportAsync(null, extractTime));
        }

        [Test]
        public void WriteReportAsync_WithEmptyOutputPath_ThrowsException()
        {
            _settings.CsvOutputPath = string.Empty;
            var positions = CreateTestPositions();
            var extractTime = DateTime.Now;

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _writer.WriteReportAsync(positions, extractTime));
        }

        [Test]
        public async Task WriteReportAsync_CreatesOutputDirectoryIfNotExists()
        {
            var newPath = Path.Combine(_testOutputPath, "NewSubDirectory");
            _settings.CsvOutputPath = newPath;
            var positions = CreateTestPositions();
            var extractTime = DateTime.Now;

            await _writer.WriteReportAsync(positions, extractTime);

            Assert.That(Directory.Exists(newPath), Is.True);
        }

        [Test]
        public async Task WriteReportAsync_HandlesDecimalVolumes()
        {
            var positions = new List<AggregatedPowerPosition>
            {
                new AggregatedPowerPosition { Hour = 23, Volume = 150.5 }
            };
            var extractTime = DateTime.Now;

            var filePath = await _writer.WriteReportAsync(positions, extractTime);
            var lines = File.ReadAllLines(filePath);

            Assert.That(lines[1], Is.EqualTo("23:00,150.5"));
        }

        [Test]
        public async Task WriteReportAsync_ReturnsFullFilePath()
        {
            var positions = CreateTestPositions();
            var extractTime = new DateTime(2024, 1, 15, 9, 30, 0);

            var filePath = await _writer.WriteReportAsync(positions, extractTime);

            Assert.That(filePath, Is.EqualTo(Path.Combine(_testOutputPath, "PowerPosition_20240115_0930.csv")));
        }

        private static List<AggregatedPowerPosition> CreateTestPositions()
        {
            return new List<AggregatedPowerPosition>
            {
                new AggregatedPowerPosition { Hour = 23, Volume = 100 },
                new AggregatedPowerPosition { Hour = 0, Volume = 100 },
                new AggregatedPowerPosition { Hour = 1, Volume = 100 }
            };
        }
    }
}
