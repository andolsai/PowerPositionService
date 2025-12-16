using NUnit.Framework;
using PowerPositionService.Core.Models;

namespace PowerPositionService.Tests
{
    [TestFixture]
    public class AggregatedPowerPositionTests
    {
        [Test]
        public void FormattedLocalTime_Returns24HourFormat()
        {
            var position = new AggregatedPowerPosition { Hour = 13 };
            Assert.That(position.FormattedLocalTime, Is.EqualTo("13:00"));
        }

        [Test]
        public void FormattedLocalTime_WithMidnight_Returns0000()
        {
            var position = new AggregatedPowerPosition { Hour = 0 };
            Assert.That(position.FormattedLocalTime, Is.EqualTo("00:00"));
        }

        [Test]
        public void FormattedLocalTime_WithSingleDigitHour_IncludesLeadingZero()
        {
            var position = new AggregatedPowerPosition { Hour = 9 };
            Assert.That(position.FormattedLocalTime, Is.EqualTo("09:00"));
        }

        [Test]
        [TestCase(23, "23:00")]
        [TestCase(0, "00:00")]
        [TestCase(1, "01:00")]
        [TestCase(12, "12:00")]
        [TestCase(22, "22:00")]
        public void FormattedLocalTime_VariousHours_ReturnsExpectedFormat(int hour, string expected)
        {
            var position = new AggregatedPowerPosition { Hour = hour };
            Assert.That(position.FormattedLocalTime, Is.EqualTo(expected));
        }
    }
}
