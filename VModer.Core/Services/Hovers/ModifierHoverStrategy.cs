using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Services.Hovers;

public sealed class ModifierHoverStrategy : IHoverStrategy
{
    public GameFileType FileType => GameFileType.Unknown;

    private readonly ModifierDisplayService _modifierDisplayService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ModifierHoverStrategy(ModifierDisplayService modifierDisplayService)
    {
        _modifierDisplayService = modifierDisplayService;
    }

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        return GetModifierDisplayText(rootNode, request);
    }

    private string GetModifierDisplayText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var node = rootNode.FindAdjacentNodeByPosition(localPosition);
        Log.Debug("光标所在 Node, Key:{Key}, Pos: {Pos}", node.Key, localPosition);

        if (!IsModifierNode(node, request))
        {
            return string.Empty;
        }

        var builder = new MarkdownDocument();
        var child = node.FindPointedChildByPosition(localPosition);
        if (child.TryGetNode(out var childNode))
        {
            foreach (
                string description in _modifierDisplayService.GetDescription(GetModifiersForNode(childNode))
            )
            {
                builder.AppendParagraph(description);
            }
        }
        else if (child.TryGetLeaf(out var leaf))
        {
            var leafModifier = LeafModifier.FromLeaf(leaf);
            builder.AppendParagraph(_modifierDisplayService.GetDescription(leafModifier));
        }

        return builder.ToString();
    }

    private static bool IsModifierNode(Node node, HoverParams request)
    {
        var fileType = GameFileType.FromFilePath(request.TextDocument.Uri.Uri.ToSystemPath());
        return fileType == GameFileType.Modifiers
            || node.Key.Equals("modifier", StringComparison.OrdinalIgnoreCase)
            || node.Key.Equals("modifiers", StringComparison.OrdinalIgnoreCase)
            || node.Key.Equals(Keywords.HiddenModifier, StringComparison.OrdinalIgnoreCase);
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
