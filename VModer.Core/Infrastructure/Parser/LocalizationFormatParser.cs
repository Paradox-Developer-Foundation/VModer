using System.Diagnostics.CodeAnalysis;
using Pidgin;
using static Pidgin.Parser;

namespace VModer.Core.Infrastructure.Parser;

public static class LocalizationFormatParser
{
    private static readonly Parser<char, char> CharExcept = Parser<char>.Token(c => c != '$');

    private static readonly Parser<char, LocalizationFormatInfo> PlaceholderParser = Char('$')
        .Then(CharExcept.AtLeastOnceString())
        .Before(Char('$'))
        .Map(placeholder => new LocalizationFormatInfo(placeholder, LocalizationFormatType.Placeholder));

    private static readonly Parser<char, LocalizationFormatInfo> TextWithColorParser = Parser<char>
        .Token(c => c != '§')
        .AtLeastOnceString()
        .Optional()
        .Between(Char('§'), String("§!"))
        .Map(text => new LocalizationFormatInfo(
            text.HasValue ? text.Value : string.Empty,
            LocalizationFormatType.TextWithColor
        ));

    private static readonly Parser<char, LocalizationFormatInfo> IconParser = Char('£')
        .Then(Parser<char>.Any.Until(OneOf(Char(' '), Char('!'), Char('£'))))
        .Map(text => new LocalizationFormatInfo(string.Concat(text), LocalizationFormatType.Icon));

    private static readonly Parser<char, LocalizationFormatInfo> TextParser =
        from text in Try(String("$$").WithResult('$')).Or(AnyCharExcept('$', '§', '£')).AtLeastOnceString()
        select new LocalizationFormatInfo(text, LocalizationFormatType.Text);

    private static readonly Parser<char, IEnumerable<LocalizationFormatInfo>> LocalizationTextParser =
        IconParser.Or(TextParser).Or(PlaceholderParser).Or(TextWithColorParser).Many();

    public static bool TryParse(
        string input,
        [NotNullWhen(true)] out IEnumerable<LocalizationFormatInfo>? formats
    )
    {
        var parseResult = LocalizationTextParser.Parse(input);
        formats = parseResult.Success ? parseResult.Value : null;

        return parseResult.Success;
    }
}
