using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
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
    private readonly ModifierDisplayService _modifierDisplayService;
    private readonly LeaderTraitsService _leaderTraitsService;
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly CharacterTraitsService _characterTraitsService;

    private const int CharacterTypeLevel = 3;

    public CharacterHoverStrategy(
        LocalizationService localizationService,
        ModifierDisplayService modifierDisplayService,
        LeaderTraitsService leaderTraitsService,
        CharacterTraitsService characterTraitsService,
        LocalizationFormatService localizationFormatService
    )
    {
        _localizationService = localizationService;
        _modifierDisplayService = modifierDisplayService;
        _leaderTraitsService = leaderTraitsService;
        _characterTraitsService = characterTraitsService;
        _localizationFormatService = localizationFormatService;
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
        else
        {
            result = GetCharacterDisplayTextByType(adjacentNode);
        }

        return result;
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
                AddGeneralTraits(childNode, builder);
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

    private void AddGeneralTraits(Node node, MarkdownDocument builder)
    {
        AddTraitsDescription(
            node,
            builder,
            traitKey =>
            {
                _characterTraitsService.TryGetTrait(traitKey, out var trait);
                return trait?.AllModifiers;
            }
        );
    }

    private string GetAdvisorDisplayText(Node node)
    {
        var builder = new MarkdownDocument();

        builder.AppendHeader(Resources.Character_advisor, CharacterTypeLevel);
        foreach (var child in node.AllArray)
        {
            if (child.TryGetNode(out var childNode))
            {
                AddLeaderTraits(childNode, builder);
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

    private void AddLeaderTraits(Node node, MarkdownDocument builder)
    {
        AddTraitsDescription(
            node,
            builder,
            traitKey =>
            {
                _leaderTraitsService.TryGetValue(traitKey, out var trait);
                return trait?.Modifiers;
            }
        );
    }

    private void AddTraitsDescription(
        Node node,
        MarkdownDocument builder,
        Func<string, IEnumerable<IModifier>?> modifiersFactory
    )
    {
        if (!node.Key.Equals("traits", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var traits = node.LeafValues.Select(trait => trait.Key);
        builder.AppendHeader($"{Resources.Traits}:", 4);

        foreach (string traitKey in traits)
        {
            // 有可能需要解引用
            builder.AppendListItem(_localizationFormatService.GetFormatText(traitKey));
            var modifiers = modifiersFactory(traitKey);
            if (modifiers is not null)
            {
                var infos = _modifierDisplayService.GetDescription(modifiers);
                foreach (string info in infos)
                {
                    builder.AppendListItem(
                        info,
                        info.StartsWith(ModifierDisplayService.NodeModifierChildrenPrefix) ? 2 : 1
                    );
                }
            }
        }
        builder.AppendHorizontalRule();
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
            AddLeaderTraits(traits, builder);
        }

        return builder.ToString();
    }
}
