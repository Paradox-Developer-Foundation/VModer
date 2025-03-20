using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Markdown;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;
using VModer.Languages;

namespace VModer.Core.Services.Hovers;

public sealed class TechnologyHoverStrategy(
    ModifierDisplayService modifierDisplayService,
    UnitService unitService,
    LocalizationFormatService localizationFormatService
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

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var adjacentNode = rootNode.FindAdjacentNodeByPosition(localPosition);

        var pointedChild = adjacentNode.FindPointedChildByPosition(localPosition);

        var builder = new MarkdownDocument();
        if (pointedChild.TryGetNode(out var node) && rootNode.IsItemNode("technologies", adjacentNode))
        {
            GetModifiersForTechnologyNode(rootNode, node, builder);
        }
        else
        {
            ProcessChildForModifiers(pointedChild, adjacentNode, rootNode, builder);
        }

        return builder.ToString();
    }

    private void GetModifiersForTechnologyNode(Node rootNode, Node node, MarkdownDocument builder)
    {
        foreach (var child in node.AllArray)
        {
            ProcessChildForModifiers(child, node, rootNode, builder);
        }
    }

    private void ProcessChildForModifiers(Child child, Node parent, Node rootNode, MarkdownDocument builder)
    {
        if (
            child.TryGetLeaf(out var leaf)
            && !Array.Exists(
                LeafKeywords,
                keyword => keyword.Equals(leaf.Key, StringComparison.OrdinalIgnoreCase)
            )
            && (unitService.Contains(parent.Key) || rootNode.IsItemNode("technologies", parent))
        )
        {
            AddDescription(modifierDisplayService.GetDescription(LeafModifier.FromLeaf(leaf)));
        }
        else if (child.TryGetNode(out var node))
        {
            if (unitService.Contains(node.Key))
            {
                foreach (
                    string description in modifierDisplayService.GetDescription(NodeModifier.FromNode(node))
                )
                {
                    AddDescription(description);
                }
            }
            else if (node.Key.Equals("categories", StringComparison.OrdinalIgnoreCase))
            {
                builder.Insert(0, new MarkdownHorizontalRule());
                foreach (var leafValue in node.LeafValues.Reverse())
                {
                    builder.Insert(
                        0,
                        new MarkdownListItem(localizationFormatService.GetFormatText(leafValue.ValueText), 1)
                    );
                }

                builder.Insert(0, new MarkdownHeader(Resources.Categories, 3));
            }
        }

        return;

        void AddDescription(string description)
        {
            builder.AppendParagraph(
                description.StartsWith(ModifierDisplayService.NodeModifierChildrenPrefix)
                    ? $"- {description}"
                    : description
            );
        }
    }
}
