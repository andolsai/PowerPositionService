using NUnit.Framework;
using PowerPositionService.Core.Configuration;

namespace PowerPositionService.Tests
{
    [TestFixture]
    public class PowerPositionSettingsTests
    {
        [Test]
        public void DefaultValues_AreSet()
        {
            var settings = new PowerPositionSettings();

            Assert.That(settings.CsvOutputPath, Is.Empty);
            Assert.That(settings.ExtractIntervalMinutes, Is.EqualTo(60));
            Assert.That(settings.MaxRetryAttempts, Is.EqualTo(3));
            Assert.That(settings.RetryDelaySeconds, Is.EqualTo(10));
        }

        [Test]
        public void SectionName_IsCorrect()
        {
            Assert.That(PowerPositionSettings.SectionName, Is.EqualTo("PowerPositionSettings"));
        }

        [Test]
        public void Properties_CanBeSet()
        {
            var settings = new PowerPositionSettings
            {
                CsvOutputPath = "/test/path",
                ExtractIntervalMinutes = 30,
                MaxRetryAttempts = 5,
                RetryDelaySeconds = 15
            };

            Assert.That(settings.CsvOutputPath, Is.EqualTo("/test/path"));
            Assert.That(settings.ExtractIntervalMinutes, Is.EqualTo(30));
            Assert.That(settings.MaxRetryAttempts, Is.EqualTo(5));
            Assert.That(settings.RetryDelaySeconds, Is.EqualTo(15));
        }
    }
}
