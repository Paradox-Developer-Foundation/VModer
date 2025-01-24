using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.Core.Services.Hovers;

public sealed class CountriesDefineHoverStrategy(LocalizationFormatService localizationFormatService)
    : IHoverStrategy
{
    public GameFileType FileType => GameFileType.CountryDefine;

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        var adjacentNode = rootNode.FindAdjacentNodeByPosition(request.Position.ToLocalPosition());
        if (!adjacentNode.Key.Equals("set_technology", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var child = adjacentNode.FindPointedChildByPosition(request.Position.ToLocalPosition());

        string result = string.Empty;
        if (child.TryGetNode(out var node))
        {
            result = GetNodeHoverText(node);
        }
        else if (child.TryGetLeaf(out var leaf))
        {
            result = GetLeafHoverText(leaf);
        }

        return result;
    }

    private string GetNodeHoverText(Node node)
    {
        var builder = new MarkdownDocument();
        foreach (var leaf in node.Leaves)
        {
            builder.AppendParagraph(localizationFormatService.GetFormatText(leaf.Key));
        }

        return builder.ToString();
    }

    private string GetLeafHoverText(Leaf leaf)
    {
        var builder = new MarkdownDocument();
        builder.AppendParagraph(localizationFormatService.GetFormatText(leaf.Key));

        if (localizationFormatService.TryGetFormatText($"{leaf.Key}_desc", out string? description))
        {
            builder.AppendHorizontalRule();
            builder.AppendParagraph(description);
        }

        return builder.ToString();
    }
}
