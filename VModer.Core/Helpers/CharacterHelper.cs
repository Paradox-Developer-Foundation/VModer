using Markdown;
using VModer.Core.Extensions;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Helpers;

public static class CharacterHelper
{
    public static string ToMarkdown(IEnumerable<string> descriptions)
    {
        var markdown = new MarkdownDocument();
        foreach (string desc in descriptions)
        {
            if (desc.StartsWith(ModifierDisplayService.NodeModifierChildrenPrefix))
            {
                markdown.AppendListItem(desc, 0);
            }
            else
            {
                markdown.AppendParagraph(desc);
            }
        }

        return markdown.ToString();
    }
}
