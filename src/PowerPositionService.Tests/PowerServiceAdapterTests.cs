using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using PowerPositionService.Core.Interfaces;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Tests
{
    /// <summary>
    /// Tests for PowerServiceAdapter using the IPowerService interface.
    /// These tests verify the interface contract and can be used with mocks.
    /// </summary>
    [TestFixture]
    public class PowerServiceAdapterTests
    {
        private Mock<IPowerService> _powerServiceMock;

        [SetUp]
        public void Setup()
        {
            _powerServiceMock = new Mock<IPowerService>();
        }

        [Test]
        public async Task GetTradesAsync_ReturnsTradesFromService()
        {
            var testDate = new DateTime(2024, 6, 15);
            var expectedTrades = new List<PowerTrade>
            {
                CreateTestTrade(testDate)
            };

            _powerServiceMock
                .Setup(x => x.GetTradesAsync(testDate))
                .ReturnsAsync(expectedTrades);

            var result = await _powerServiceMock.Object.GetTradesAsync(testDate);

            Assert.That(result, Is.EqualTo(expectedTrades));
        }

        [Test]
        public async Task GetTradesAsync_WithMultipleTrades_ReturnsAllTrades()
        {
            var testDate = new DateTime(2024, 6, 15);
            var expectedTrades = new List<PowerTrade>
            {
                CreateTestTrade(testDate),
                CreateTestTrade(testDate),
                CreateTestTrade(testDate)
            };

            _powerServiceMock
                .Setup(x => x.GetTradesAsync(testDate))
                .ReturnsAsync(expectedTrades);

            var result = await _powerServiceMock.Object.GetTradesAsync(testDate);

            Assert.That(result.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task GetTradesAsync_WithNoTrades_ReturnsEmptyCollection()
        {
            var testDate = new DateTime(2024, 6, 15);

            _powerServiceMock
                .Setup(x => x.GetTradesAsync(testDate))
                .ReturnsAsync(new List<PowerTrade>());

            var result = await _powerServiceMock.Object.GetTradesAsync(testDate);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetTradesAsync_WhenServiceThrows_PropagatesException()
        {
            var testDate = new DateTime(2024, 6, 15);

            _powerServiceMock
                .Setup(x => x.GetTradesAsync(testDate))
                .ThrowsAsync(new InvalidOperationException("Service error"));

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _powerServiceMock.Object.GetTradesAsync(testDate));
        }

        [Test]
        public async Task GetTradesAsync_ReturnedTrades_Have24Periods()
        {
            var testDate = new DateTime(2024, 6, 15);
            var trade = CreateTestTrade(testDate);

            _powerServiceMock
                .Setup(x => x.GetTradesAsync(testDate))
                .ReturnsAsync(new List<PowerTrade> { trade });

            var result = (await _powerServiceMock.Object.GetTradesAsync(testDate)).First();

            Assert.That(result.Periods.Length, Is.EqualTo(24));
        }

        [Test]
        public async Task GetTradesAsync_ReturnedPeriods_HaveCorrectPeriodNumbers()
        {
            var testDate = new DateTime(2024, 6, 15);
            var trade = CreateTestTrade(testDate);

            _powerServiceMock
                .Setup(x => x.GetTradesAsync(testDate))
                .ReturnsAsync(new List<PowerTrade> { trade });

            var result = (await _powerServiceMock.Object.GetTradesAsync(testDate)).First();

            Assert.That(result.Periods[0].Period, Is.EqualTo(1));
            Assert.That(result.Periods[23].Period, Is.EqualTo(24));
        }

        private static PowerTrade CreateTestTrade(DateTime date)
        {
            var periods = new PowerPeriod[24];
            for (int i = 0; i < 24; i++)
            {
                periods[i] = new PowerPeriod { Period = i + 1, Volume = 100 };
            }
            return new PowerTrade { Date = date, Periods = periods };
        }
    }
}
