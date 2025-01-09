using Markdown;
using VModer.Core.Infrastructure.Markdown;

namespace VModer.Core.Extensions;

public static class MarkdownExtensions
{
    /// <summary>
    /// 添加一个列表元素
    /// </summary>
    /// <param name="document"></param>
    /// <param name="text"></param>
    /// <param name="level">层级</param>
    /// <exception cref="ArgumentOutOfRangeException">如果<c>level</c>小于0</exception>
    public static IMarkdownDocument AppendListItem(this MarkdownDocument document, string text, int level = 0)
    {
        return document.Append(new MarkdownListItem(text, level));
    }
}