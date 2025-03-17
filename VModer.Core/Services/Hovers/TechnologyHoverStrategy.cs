using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Services.Hovers;

public sealed class TechnologyHoverStrategy(
    ModifierDisplayService modifierDisplayService,
    UnitService unitService
) : IHoverStrategy
{
    public GameFileType FileType => GameFileType.Technology;

    // https://hoi4.paradoxwikis.com/Technology_modding#Modifiers
    private static readonly string[] LeafKeywords =
    [
        "research_cost",
        "start_year",
        "show_equipment_icon",
        "force_use_small_tech_layout",
        "doctrine",
        "doctrine_name",
        "is_special_project_tech",
        "desc"
    ];

    // TODO: 当鼠标放到一个非 Modifier Leaf 上时, 会错误的显示描述
    public string GetHoverText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var adjacentNode = rootNode.FindAdjacentNodeByPosition(localPosition);

        var pointedChild = adjacentNode.FindPointedChildByPosition(localPosition);

        var modifiers = new List<IModifier>(4);
        if (pointedChild.TryGetNode(out var node) && rootNode.IsItemNode("technologies", adjacentNode))
        {
            GetModifiersForNode(node, modifiers);
        }
        else
        {
            ProcessChildForModifiers(pointedChild, modifiers);
        }

        var descriptions = modifierDisplayService.GetDescription(modifiers);
        var builder = new MarkdownDocument(descriptions.Count);
        foreach (string modifier in descriptions)
        {
            builder.AppendParagraph(
                modifier.StartsWith(ModifierDisplayService.NodeModifierChildrenPrefix)
                    ? $"- {modifier}"
                    : modifier
            );
        }

        return builder.ToString();
    }

    private void GetModifiersForNode(Node node, List<IModifier> modifiers)
    {
        foreach (var child in node.AllArray)
        {
            ProcessChildForModifiers(child, modifiers);
        }
    }

    private void ProcessChildForModifiers(Child child, List<IModifier> modifiers)
    {
        if (
            child.TryGetLeaf(out var leaf)
            && !Array.Exists(
                LeafKeywords,
                keyword => keyword.Equals(leaf.Key, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            modifiers.Add(LeafModifier.FromLeaf(leaf));
        }
        else if (child.TryGetNode(out var childNode) && unitService.Contains(childNode.Key))
        {
            modifiers.Add(NodeModifier.FromNode(childNode));
        }
    }
}
