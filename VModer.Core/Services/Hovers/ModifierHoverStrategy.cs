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

        if (
            !node.Key.Equals("modifier", StringComparison.OrdinalIgnoreCase)
            // ideologies 下会使用 modifiers 而不是 modifier
            && !node.Key.Equals("modifiers", StringComparison.OrdinalIgnoreCase)
        )
        {
            return string.Empty;
        }

        var child = node.FindChildByPosition(localPosition);
        IEnumerable<IModifier> modifiers = [];
        if (child.TryGetNode(out var childNode))
        {
            modifiers = GetModifiersForNode(childNode);
        }
        else if (child.TryGetLeaf(out var leaf))
        {
            modifiers = [LeafModifier.FromLeaf(leaf)];
        }

        var builder = new MarkdownDocument();
        foreach (string modifierInfo in _modifierDisplayService.GetDescription(modifiers))
        {
            builder.AppendParagraph(modifierInfo);
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
