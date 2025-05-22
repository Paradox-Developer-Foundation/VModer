using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Helpers;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Services.Hovers;

public sealed class ModifierHoverStrategy(ModifierDisplayService modifierDisplayService) : IHoverStrategy
{
    public GameFileType FileType => GameFileType.Modifiers;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        return GetModifierDisplayText(rootNode, request);
    }

    private string GetModifierDisplayText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var node = rootNode.FindAdjacentNodeByPosition(localPosition);
        Log.Debug("光标所在 Node, Key:{Key}, Pos: {Pos}", node.Key, localPosition);

        if (
            !ModifierHelper.IsModifierNode(node, request)
            || node.Parent?.Key.EqualsIgnoreCase("ai_will_do") == true
        )
        {
            return string.Empty;
        }

        var builder = new MarkdownDocument();
        var child = node.FindPointedChildByPosition(localPosition);
        if (child.TryGetNode(out var childNode))
        {
            foreach (
                string description in modifierDisplayService.GetDescription(GetModifiersForNode(childNode))
            )
            {
                builder.AppendParagraph(description);
            }
        }
        else if (child.TryGetLeaf(out var leaf))
        {
            var leafModifier = LeafModifier.FromLeaf(leaf);
            builder.AppendParagraph(modifierDisplayService.GetDescription(leafModifier));
        }

        return builder.ToString();
    }

    private static List<IModifier> GetModifiersForNode(Node node)
    {
        var modifiers = new List<IModifier>();
        foreach (var child in node.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                modifiers.Add(LeafModifier.FromLeaf(leaf));
            }
            else if (child.TryGetNode(out var childNode))
            {
                modifiers.Add(NodeModifier.FromNode(childNode));
            }
        }

        return modifiers;
    }
}
