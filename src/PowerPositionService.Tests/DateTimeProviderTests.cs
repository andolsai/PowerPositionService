using System;
using NUnit.Framework;
using PowerPositionService.Core.Services;

namespace PowerPositionService.Tests
{
    [TestFixture]
    public class DateTimeProviderTests
    {
        private DateTimeProvider _provider;

        [SetUp]
        public void Setup()
        {
            _provider = new DateTimeProvider();
        }

        [Test]
        public void UtcNow_ReturnsCurrentUtcTime()
        {
            var result = _provider.UtcNow;
            Assert.That(result, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void LondonNow_ReturnsTimeInLondonTimezone()
        {
            var londonTime = _provider.LondonNow;
            var utcTime = _provider.UtcNow;

            var difference = (londonTime - utcTime).TotalHours;
            Assert.That(difference, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(1));
        }

        [Test]
        public void ConvertToLondon_DuringWinter_ReturnsUtcTime()
        {
            var utcTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            var londonTime = _provider.ConvertToLondon(utcTime);
            Assert.That(londonTime.Hour, Is.EqualTo(12));
        }

        [Test]
        public void ConvertToLondon_DuringSummer_ReturnsUtcPlusOne()
        {
            var utcTime = new DateTime(2024, 7, 15, 12, 0, 0, DateTimeKind.Utc);
            var londonTime = _provider.ConvertToLondon(utcTime);
            Assert.That(londonTime.Hour, Is.EqualTo(13));
        }
    }
}
