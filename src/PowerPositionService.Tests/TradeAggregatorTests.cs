using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PowerPositionService.Core.Models;
using PowerPositionService.Core.Services;

namespace PowerPositionService.Tests
{
    [TestFixture]
    public class TradeAggregatorTests
    {
        private Mock<ILogger<TradeAggregator>> _loggerMock;
        private TradeAggregator _aggregator;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<TradeAggregator>>();
            _aggregator = new TradeAggregator(_loggerMock.Object);
        }

        [Test]
        public void AggregateTrades_WithNullInput_ReturnsEmptyCollection()
        {
            var result = _aggregator.AggregateTrades(null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void AggregateTrades_WithEmptyInput_ReturnsEmptyCollection()
        {
            var trades = Enumerable.Empty<PowerTrade>();
            var result = _aggregator.AggregateTrades(trades);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void AggregateTrades_WithSingleTrade_ReturnsCorrectPositions()
        {
            var trade = CreateTestTrade(DateTime.Today, 100);
            var result = _aggregator.AggregateTrades(new[] { trade }).ToList();

            Assert.That(result, Has.Count.EqualTo(24));
            Assert.That(result.All(p => p.Volume == 100), Is.True);
        }

        [Test]
        public void AggregateTrades_WithMultipleTrades_AggregatesVolumes()
        {
            var trade1 = CreateTestTrade(DateTime.Today, 100);
            var trade2 = CreateTestTrade(DateTime.Today, 50);

            var result = _aggregator.AggregateTrades(new[] { trade1, trade2 }).ToList();

            Assert.That(result, Has.Count.EqualTo(24));
            Assert.That(result.All(p => p.Volume == 150), Is.True);
        }

        [Test]
        public void AggregateTrades_Period1MapsTo2300()
        {
            var trade = CreateTradeWithSinglePeriod(DateTime.Today, 1, 100);
            var result = _aggregator.AggregateTrades(new[] { trade }).ToList();

            var period1Position = result.First(p => p.Hour == 23);
            Assert.That(period1Position.Volume, Is.EqualTo(100));
        }

        [Test]
        public void AggregateTrades_Period2MapsTo0000()
        {
            var trade = CreateTradeWithSinglePeriod(DateTime.Today, 2, 200);
            var result = _aggregator.AggregateTrades(new[] { trade }).ToList();

            var period2Position = result.First(p => p.Hour == 0);
            Assert.That(period2Position.Volume, Is.EqualTo(200));
        }

        [Test]
        public void AggregateTrades_Period24MapsTo2200()
        {
            var trade = CreateTradeWithSinglePeriod(DateTime.Today, 24, 300);
            var result = _aggregator.AggregateTrades(new[] { trade }).ToList();

            var period24Position = result.First(p => p.Hour == 22);
            Assert.That(period24Position.Volume, Is.EqualTo(300));
        }

        [Test]
        public void AggregateTrades_ResultsAreSortedStartingFrom2300()
        {
            var trade = CreateTestTrade(DateTime.Today, 100);
            var result = _aggregator.AggregateTrades(new[] { trade }).ToList();

            Assert.That(result.First().Hour, Is.EqualTo(23));
            Assert.That(result.Last().Hour, Is.EqualTo(22));
        }

        [Test]
        public void AggregateTrades_WithExampleFromRequirements_ReturnsExpectedOutput()
        {
            // Trade 1: 100 for all periods
            var trade1 = CreateTestTradeWithVolumes(DateTime.Parse("2015-04-01"),
                Enumerable.Repeat(100.0, 24).ToArray());
            
            // Trade 2: 50 for first 11 periods, -20 for last 13 periods
            var trade2Volumes = Enumerable.Repeat(50.0, 11)
                .Concat(Enumerable.Repeat(-20.0, 13))
                .ToArray();
            var trade2 = CreateTestTradeWithVolumes(DateTime.Parse("2015-04-01"), trade2Volumes);

            var result = _aggregator.AggregateTrades(new[] { trade1, trade2 }).ToList();

            Assert.That(result, Has.Count.EqualTo(24));
            
            // First 11 periods (23:00 to 09:00) should have volume 150
            var first11 = result.Take(11).ToList();
            Assert.That(first11.All(p => p.Volume == 150), Is.True,
                string.Format("Expected 150, got: {0}", string.Join(", ", first11.Select(p => p.Volume))));
            
            // Remaining 13 periods (10:00 to 22:00) should have volume 80
            var last13 = result.Skip(11).ToList();
            Assert.That(last13.All(p => p.Volume == 80), Is.True,
                string.Format("Expected 80, got: {0}", string.Join(", ", last13.Select(p => p.Volume))));
        }

        [Test]
        public void AggregateTrades_WithNegativeVolumes_AggregatesCorrectly()
        {
            var trade1 = CreateTestTrade(DateTime.Today, 100);
            var trade2 = CreateTestTrade(DateTime.Today, -30);

            var result = _aggregator.AggregateTrades(new[] { trade1, trade2 }).ToList();

            Assert.That(result.All(p => p.Volume == 70), Is.True);
        }

        private static PowerTrade CreateTestTrade(DateTime date, double volume)
        {
            var periods = new PowerPeriod[24];
            for (int i = 0; i < 24; i++)
            {
                periods[i] = new PowerPeriod { Period = i + 1, Volume = volume };
            }
            return new PowerTrade { Date = date, Periods = periods };
        }

        private static PowerTrade CreateTradeWithSinglePeriod(DateTime date, int period, double volume)
        {
            return new PowerTrade
            {
                Date = date,
                Periods = new[] { new PowerPeriod { Period = period, Volume = volume } }
            };
        }

        private static PowerTrade CreateTestTradeWithVolumes(DateTime date, double[] volumes)
        {
            var periods = new PowerPeriod[volumes.Length];
            for (int i = 0; i < volumes.Length; i++)
            {
                periods[i] = new PowerPeriod { Period = i + 1, Volume = volumes[i] };
            }
            return new PowerTrade { Date = date, Periods = periods };
        }
    }
}
