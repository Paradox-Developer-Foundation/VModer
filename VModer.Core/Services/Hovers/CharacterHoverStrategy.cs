using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Models.Character;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;
using VModer.Languages;

namespace VModer.Core.Services.Hovers;

public sealed class CharacterHoverStrategy : IHoverStrategy
{
    public GameFileType FileType => GameFileType.Character;

    private readonly LocalizationService _localizationService;
    private readonly ModifierService _modifierService;
    private readonly ModifierDisplayService _modifierDisplayService;
    private readonly LeaderTraitsService _leaderTraitsService;
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly CharacterTraitsService _characterTraitsService;

    private const int CharacterTypeLevel = 3;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CharacterHoverStrategy(
        LocalizationService localizationService,
        ModifierDisplayService modifierDisplayService,
        LeaderTraitsService leaderTraitsService,
        CharacterTraitsService characterTraitsService,
        LocalizationFormatService localizationFormatService,
        ModifierService modifierService
    )
    {
        _localizationService = localizationService;
        _modifierDisplayService = modifierDisplayService;
        _leaderTraitsService = leaderTraitsService;
        _characterTraitsService = characterTraitsService;
        _localizationFormatService = localizationFormatService;
        _modifierService = modifierService;
    }

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        return GetCharacterDisplayText(rootNode, request);
    }

    private string GetCharacterDisplayText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var adjacentNode = rootNode.FindAdjacentNodeByPosition(localPosition);

        string result;
        // 当是人物节点时, 逐一进行分析并显示内容
        if (IsCharacterNode(rootNode, adjacentNode))
        {
            var builder = new MarkdownDocument();

            AddCharacterNameTitle(builder, adjacentNode);

            foreach (var node in adjacentNode.Nodes)
            {
                string text = GetCharacterDisplayTextByType(node);
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                builder.AppendParagraph(text);
                builder.AppendHorizontalRule();
            }

            if (builder.Length != 0 && builder.ElementAt(builder.Length - 1) is MarkdownHorizontalRule)
            {
                builder.Remove(builder.Length - 1);
            }
            result = builder.ToString();
        }
        else if (adjacentNode.Key.Equals("traits", StringComparison.OrdinalIgnoreCase))
        {
            result = GetTraitsDisplayText(adjacentNode, localPosition);
        }
        else
        {
            result = GetCharacterDisplayTextByType(adjacentNode);
        }

        return result;
    }

    private string GetTraitsDisplayText(Node adjacentNode, LocalPosition localPosition)
    {
        var builder = new MarkdownDocument();
        var child = adjacentNode.FindPointedChildByPosition(localPosition);

        // 当光标放在某一个特质上时
        if (child.TryGetLeafValue(out var traitName))
        {
            var node = Node.Create("traits");
            node.AddChild(traitName);

            AddTraitsDescription(node, builder, LookUpTraitType.All);
        }
        // 当光标放在某一个特质列表上时
        else if (child.TryGetNode(out var traitsNode))
        {
            AddTraitsDescription(traitsNode, builder, LookUpTraitType.All);
        }

        return builder.ToString();
    }

    private static bool IsCharacterNode(Node rootNode, Node node)
    {
        var charactersNodes = rootNode.Nodes.Where(n =>
            n.Key.Equals("characters", StringComparison.OrdinalIgnoreCase)
        );
        return charactersNodes.Any(charactersNode =>
            charactersNode.Nodes.Any(character => character.Position.Equals(node.Position))
        );
    }

    private void AddCharacterNameTitle(MarkdownDocument builder, Node characterNode)
    {
        var name = characterNode.Leaves.FirstOrDefault(leaf =>
            leaf.Key.Equals("name", StringComparison.OrdinalIgnoreCase)
        );

        string nameText = name is null
            ? _localizationFormatService.GetFormatText(characterNode.Key)
            : _localizationFormatService.GetFormatText(name.ValueText);
        builder.AppendHeader(nameText, 2);
        builder.AppendHorizontalRule();
    }

    private string GetCharacterDisplayTextByType(Node node)
    {
        string result = string.Empty;
        if (
            Array.Exists(
                Keywords.GeneralKeywords,
                keyword => keyword.Equals(node.Key, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            result = GetGeneralDisplayText(node);
        }
        else if (node.Key.Equals("advisor", StringComparison.OrdinalIgnoreCase))
        {
            result = GetAdvisorDisplayText(node);
        }
        else if (node.Key.Equals("country_leader", StringComparison.OrdinalIgnoreCase))
        {
            result = GetCountryLeaderDisplayText(node);
        }

        return result;
    }

    private string GetGeneralDisplayText(Node node)
    {
        var builder = new MarkdownDocument();

        builder.AppendHeader(GetGeneralTypeName(node.Key), CharacterTypeLevel);

        var skillSet = SkillType.List.ToDictionary(
            type => type.Value,
            _ => (ushort)0,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var child in node.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (skillSet.ContainsKey(leaf.Key) && ushort.TryParse(leaf.ValueText, out ushort value))
                {
                    skillSet[leaf.Key] = value;
                }
            }
            else if (child.TryGetNode(out var childNode))
            {
                AddTraitsDescription(childNode, builder, LookUpTraitType.General);
            }
        }

        var skillType = SkillCharacterType.FromCharacterType(node.Key);
        foreach (
            string skillInfo in skillSet.SelectMany(kvp =>
                _modifierDisplayService.GetSkillModifierDescription(
                    SkillType.FromValue(kvp.Key),
                    skillType,
                    kvp.Value
                )
            )
        )
        {
            builder.AppendParagraph(skillInfo);
        }

        return builder.ToString();
    }

    private static string GetGeneralTypeName(string nodeKey)
    {
        return nodeKey switch
        {
            "field_marshal" => Resources.Character_field_marshal,
            "corps_commander" => Resources.Character_corps_commander,
            "navy_leader" => Resources.Character_navy_leader,
            _ => string.Empty
        };
    }

    private string GetAdvisorDisplayText(Node node)
    {
        var builder = new MarkdownDocument();

        builder.AppendHeader(Resources.Character_advisor, CharacterTypeLevel);
        foreach (var child in node.AllArray)
        {
            if (child.TryGetNode(out var childNode))
            {
                AddTraitsDescription(childNode, builder, LookUpTraitType.Leader);
            }
            else if (child.TryGetLeaf(out var leaf))
            {
                if (leaf.Key.Equals("slot", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendParagraph(
                        $"{Resources.Character_advisor_slot}: {_localizationService.GetValue(leaf.ValueText)}"
                    );
                }
            }
        }

        return builder.ToString();
    }

    private void AddTraitsDescription(Node traitsNode, MarkdownDocument builder, LookUpTraitType type)
    {
        if (!traitsNode.Key.Equals("traits", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var traits = traitsNode.LeafValues.Select(trait => trait.Key);
        builder.AppendHeader($"{Resources.Traits}:", 4);

        foreach (string traitKey in traits)
        {
            // 有可能需要解引用
            builder.AppendListItem(_localizationFormatService.GetFormatText(traitKey));

            var modifiers = GetTraitModifiersByType(traitKey, type);
            var infos = _modifierDisplayService.GetDescription(modifiers);
            foreach (string info in infos)
            {
                builder.AppendListItem(
                    info,
                    info.StartsWith(ModifierDisplayService.NodeModifierChildrenPrefix) ? 2 : 1
                );
            }

            if (
                type != LookUpTraitType.Leader
                && _characterTraitsService.TryGetTrait(traitKey, out var trait)
            )
            {
                foreach (var modifier in trait.TraitXpModifiers.OfType<LeafModifier>())
                {
                    //TODO: 实现从本地化中读取 trait_xp_factor 的本地化值
                    builder.AppendListItem(
                        $"{_localizationFormatService.GetFormatText(modifier.Key)} {Resources.TraitXpFactor}：{_modifierService.GetDisplayValue(modifier, "H%.0")}",
                        1
                    );
                }
            }
        }
        builder.AppendHorizontalRule();
    }

    private IEnumerable<IModifier> GetTraitModifiersByType(string traitKey, LookUpTraitType type)
    {
        switch (type)
        {
            case LookUpTraitType.All:
            {
                _characterTraitsService.TryGetTrait(traitKey, out var trait);
                if (trait is not null)
                {
                    return trait.AllModifiers;
                }

                _leaderTraitsService.TryGetValue(traitKey, out var leaderTrait);
                return leaderTrait?.Modifiers ?? [];
            }
            case LookUpTraitType.General:
            {
                _characterTraitsService.TryGetTrait(traitKey, out var trait);
                return trait?.AllModifiers ?? [];
            }
            case LookUpTraitType.Leader:
            {
                _leaderTraitsService.TryGetValue(traitKey, out var trait);
                return trait?.Modifiers ?? [];
            }
            default:
                return [];
        }
    }

    private enum LookUpTraitType : byte
    {
        All,
        General,
        Leader
    }

    private string GetCountryLeaderDisplayText(Node leaderNode)
    {
        var builder = new MarkdownDocument();

        builder.AppendHeader(Resources.Character_country_leader, CharacterTypeLevel);
        var ideology = leaderNode.Leaves.FirstOrDefault(leaf =>
            leaf.Key.Equals("ideology", StringComparison.OrdinalIgnoreCase)
        );
        if (ideology is not null)
        {
            builder.AppendParagraph(
                $"{Resources.Ideology}: {_localizationService.GetValue(ideology.ValueText)}"
            );
        }

        var traits = leaderNode.Nodes.FirstOrDefault(node =>
            node.Key.Equals("traits", StringComparison.OrdinalIgnoreCase)
        );

        if (traits is not null)
        {
            AddTraitsDescription(traits, builder, LookUpTraitType.Leader);
        }

        return builder.ToString();
    }
}
