using VModer.Core.Infrastructure.Parser;

namespace VModer.UnitTests.Infrastructure.Parser;

[TestFixture]
[TestOf(typeof(LocalizationFormatParser))]
public class LocalizationFormatParserTest
{
    [Test]
    public void ParserPlainText()
    {
        {
            const string input = @"Line1\nLine2";

            bool result = LocalizationFormatParser.TryParse(input, out var formats);

            var formatList = formats!.ToList();

            Assert.That(result, Is.True);
            Assert.That(formatList, Is.Not.Null);
            Assert.That(formatList.Count, Is.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(formatList[0].Type, Is.EqualTo(LocalizationFormatType.Text));
                Assert.That(formatList[0].Text, Is.EqualTo("Line1\nLine2"));
            });
        }

        {
            const string input = @"Line1\\nLine2";

            bool result = LocalizationFormatParser.TryParse(input, out var formats);
            var formatList = formats!.ToList();

            Assert.That(result, Is.True);
            Assert.That(formatList.Count, Is.EqualTo(1));
            Assert.That(formatList[0].Type, Is.EqualTo(LocalizationFormatType.Text));
            Assert.That(formatList[0].Text, Is.EqualTo("Line1\\\nLine2"));
        }
    }

    [Test]
    public void ParserTextWithColor()
    {
        const string input = "§RLine1§!Line2";

        bool result = LocalizationFormatParser.TryParse(input, out var formats);
        var formatList = formats!.ToList();

        Assert.That(result, Is.True);
        Assert.That(formatList, Is.Not.Null);
        Assert.That(formatList.Count, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(formatList[0].Type, Is.EqualTo(LocalizationFormatType.TextWithColor));
            Assert.That(formatList[0].Text, Is.EqualTo("RLine1"));
            Assert.That(formatList[1].Type, Is.EqualTo(LocalizationFormatType.Text));
            Assert.That(formatList[1].Text, Is.EqualTo("Line2"));
        });
    }
}
