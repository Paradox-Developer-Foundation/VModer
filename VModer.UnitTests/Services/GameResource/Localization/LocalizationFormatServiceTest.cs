using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using NSubstitute;
using VModer.Core.Models;
using VModer.Core.Services;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.UnitTests.Services.GameResource.Localization;

[TestFixture]
[TestOf(typeof(LocalizationFormatService))]
public sealed class LocalizationFormatServiceTest
{
    [Test]
    [SuppressMessage(
        "Assertion",
        "NUnit2046:Use CollectionConstraint for better assertion messages in case of failure"
    )]
    public void GetFormatTextInfo()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        var localizationTextColorsService = Substitute.For<ILocalizationTextColorsService>();
        var imageService = Substitute.For<IImageService>();

        localizationTextColorsService
            .TryGetColor('R', out _)
            .Returns(info =>
            {
                info[1] = new LocalizationTextColor('R', Color.Red);
                return true;
            });
        localizationTextColorsService
            .TryGetColor('B', out _)
            .Returns(info =>
            {
                info[1] = new LocalizationTextColor('B', Color.Blue);
                return true;
            });

        localizationService.GetValue("pointer").Returns("data");
        localizationService.GetValue("pointer1").Returns("data1");
        localizationService.GetValue("pointer2").Returns("$pointer1$");
        localizationService.GetValue("pointer3").Returns("§RTest§!");

        var service = new LocalizationFormatService(
            localizationTextColorsService,
            localizationService,
            imageService
        );

        {
            var result = service.GetFormatTextInfo("§RTest§!").ToList();
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].DisplayText, Is.EqualTo("Test"));
                Assert.That(result[0].Color, Is.EqualTo(Color.Red));
            });
        }

        {
            var result = service.GetFormatTextInfo("§R$pointer$§!").ToList();
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].DisplayText, Is.EqualTo("data"));
                Assert.That(result[0].Color, Is.EqualTo(Color.Red));
            });
        }

        {
            var result = service.GetFormatTextInfo("§B$pointer$$pointer1$§!").ToList();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].DisplayText, Is.EqualTo("data"));
                Assert.That(result[0].Color, Is.EqualTo(Color.Blue));
                Assert.That(result[1].DisplayText, Is.EqualTo("data1"));
                Assert.That(result[1].Color, Is.EqualTo(Color.Blue));
            });
        }

        {
            var result = service.GetFormatTextInfo("§B$pointer$$pointer2$§!").ToList();
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].DisplayText, Is.EqualTo("data"));
                Assert.That(result[0].Color, Is.EqualTo(Color.Blue));
                Assert.That(result[1].DisplayText, Is.EqualTo("data1"));
                Assert.That(result[1].Color, Is.EqualTo(Color.Blue));
            });
        }
    }
}
