using Markdown;

namespace VModer.Core.Infrastructure.Markdown;

public sealed class MarkdownListItem : MarkdownTextElement, IMarkdownBlockElement
{
    private readonly int _level;

    public MarkdownListItem(string text, int level)
        : base(text)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);

        _level = level;
    }

    public override string ToString()
    {
        string prefix = new(' ', _level * 2);
        return $"{prefix}- {Text}";
    }
}
