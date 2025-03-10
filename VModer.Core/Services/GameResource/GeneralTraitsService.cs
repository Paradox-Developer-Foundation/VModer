using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using MethodTimer;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Dto;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure;
using VModer.Core.Models.Character;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Base;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Services.GameResource;

public sealed class GeneralTraitsService
    : CommonResourcesService<GeneralTraitsService, FrozenDictionary<string, CharacterTrait>>
{
    private readonly ResetLazy<CharacterTrait[]> _allTraitsLazy;
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly ModifierDisplayService _modifierDisplayService;
    private readonly GameResourcesPathService _gameResourcesPathService;
    private readonly ModifierService _modifierService;
    private ICollection<FrozenDictionary<string, CharacterTrait>> Traits => Resources.Values;

    /// <summary>
    /// 特质修饰符节点名称
    /// </summary>
    private static readonly string[] ModifierNodeKeys =
    [
        "modifier",
        "non_shared_modifier",
        "corps_commander_modifier",
        "field_marshal_modifier",
        "sub_unit_modifiers"
    ];

    private static readonly string[] SkillModifierKeywords =
    [
        "attack_skill",
        "defense_skill",
        "planning_skill",
        "logistics_skill",
        "maneuvering_skill",
        "coordination_skill"
    ];

    private static readonly string[] SkillFactorModifierKeywords =
    [
        "skill_factor",
        "attack_skill_factor",
        "defense_skill_factor",
        "planning_skill_factor",
        "logistics_skill_factor",
        "maneuvering_skill_factor",
        "coordination_skill_factor"
    ];

    [Time("加载将领特质")]
    public GeneralTraitsService(
        LocalizationFormatService localizationFormatService,
        ModifierDisplayService modifierDisplayService,
        GameResourcesPathService gameResourcesPathService,
        ModifierService modifierService
    )
        : base(Path.Combine(Keywords.Common, "unit_leader"), WatcherFilter.Text)
    {
        _localizationFormatService = localizationFormatService;
        _modifierDisplayService = modifierDisplayService;
        _gameResourcesPathService = gameResourcesPathService;
        _modifierService = modifierService;

        _allTraitsLazy = new ResetLazy<CharacterTrait[]>(
            () => Traits.SelectMany(trait => trait.Values).ToArray()
        );
        OnResourceChanged += (_, _) => _allTraitsLazy.Reset();
    }

    [Time("获取所有将领特质")]
    public List<TraitDto> GetAllTraitDto()
    {
        var traits = new List<TraitDto>(Resources.Sum(dic => dic.Value.Count));

        foreach (var fileResource in Resources)
        {
            var fileOrigin = _gameResourcesPathService.GetFileOrigin(fileResource.Key);
            foreach (var trait in fileResource.Value.Select(item => item.Value))
            {
                traits.Add(
                    new TraitDto
                    {
                        Name = trait.Name,
                        LocalizedName = GetLocalizationName(trait),
                        Modifiers = string.Join('\n', GetModifiersDescription(trait)),
                        FileOrigin = fileOrigin,
                        Type = trait.Type
                    }
                );
            }
        }

        return traits;
    }

    public IEnumerable<string> GetModifiersDescription(CharacterTrait trait)
    {
        var descriptions = new List<string>(8);

        foreach (var modifierCollection in trait.ModifiersCollection)
        {
            if (
                modifierCollection.Key.Equals(
                    CharacterTrait.TraitXpFactor,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                foreach (var modifier in modifierCollection.Modifiers.OfType<LeafModifier>())
                {
                    //TODO: 实现从本地化中读取 trait_xp_factor 的本地化值
                    descriptions.Add(
                        $"{_localizationFormatService.GetFormatText(modifier.Key)} {Languages.Resources.TraitXpFactor}：{_modifierService.GetDisplayValue(modifier, "H%.0")}"
                    );
                }
            }
            else
            {
                descriptions.AddRange(_modifierDisplayService.GetDescription(modifierCollection.Modifiers));
            }
        }

        return descriptions;
    }

    public bool TryGetTrait(string name, [NotNullWhen(true)] out CharacterTrait? trait)
    {
        foreach (var traitMap in Traits)
        {
            if (traitMap.TryGetValue(name, out trait))
            {
                return true;
            }
        }

        trait = null;
        return false;
    }

    private string GetLocalizationName(CharacterTrait characterTrait)
    {
        return _localizationFormatService.GetFormatText(characterTrait.Name);
    }

    protected override FrozenDictionary<string, CharacterTrait>? ParseFileToContent(Node rootNode)
    {
        // Character Traits 和 技能等级修正 在同一个文件夹中, 这里我们只处理 Character Traits 文件
        var traitsNodes = Array.FindAll(
            rootNode.AllArray,
            child =>
                child.TryGetNode(out var node)
                && StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_traits")
        );

        if (traitsNodes.Length == 0)
        {
            return null;
        }

        // 在 1.14 版本中, 人物特质文件中大约有 145 个特质
        var dictionary = new Dictionary<string, CharacterTrait>(163, StringComparer.OrdinalIgnoreCase);
        foreach (var traitsChild in traitsNodes)
        {
            traitsChild.TryGetNode(out var traitsNode);
            Debug.Assert(traitsNode is not null);
            foreach (var traits in ParseTraitsNode(traitsNode))
            {
                dictionary[traits.Name] = traits;
            }
        }

        return dictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="traitsNode">文件中的 leader_traits 节点</param>
    /// <returns></returns>
    private ReadOnlySpan<CharacterTrait> ParseTraitsNode(Node traitsNode)
    {
        var traits = new List<CharacterTrait>(traitsNode.AllArray.Length);

        foreach (var child in traitsNode.AllArray)
        {
            if (!child.TryGetNode(out var traitNode))
            {
                continue;
            }

            string traitName = traitNode.Key;

            var modifiers = new List<ModifierCollection>(4);
            var skillModifiers = new List<LeafModifier>();
            var customModifiersTooltip = new List<LeafModifier>();
            var traitType = TraitType.None;
            foreach (var traitAttribute in traitNode.AllArray)
            {
                string? key = traitAttribute.GetKeyOrNull();
                // type 可以为 Leaf 或 Node
                if (StringComparer.OrdinalIgnoreCase.Equals(key, "type"))
                {
                    traitType = GetTraitType(traitAttribute);
                }
                else if (
                    traitAttribute.TryGetNode(out var node)
                    && (
                        Array.Exists(
                            ModifierNodeKeys,
                            keyword => StringComparer.OrdinalIgnoreCase.Equals(keyword, key)
                        ) || StringComparer.OrdinalIgnoreCase.Equals(key, CharacterTrait.TraitXpFactor)
                    )
                )
                {
                    modifiers.Add(ParseModifier(node));
                }
                else if (
                    traitAttribute.TryGetLeaf(out var leaf)
                    && StringComparer.OrdinalIgnoreCase.Equals(LeafModifier.CustomEffectTooltipKey, key)
                )
                {
                    customModifiersTooltip.Add(LeafModifier.FromLeaf(leaf));
                }
                else if (IsSkillModifier(traitAttribute, out leaf))
                {
                    skillModifiers.Add(LeafModifier.FromLeaf(leaf));
                }
            }

            if (skillModifiers.Count != 0)
            {
                modifiers.Add(new ModifierCollection(CharacterTrait.TraitSkillModifiersKey, skillModifiers));
            }

            if (customModifiersTooltip.Count != 0)
            {
                modifiers.Add(
                    new ModifierCollection(LeafModifier.CustomEffectTooltipKey, customModifiersTooltip)
                );
            }
            traits.Add(new CharacterTrait(traitName, traitType, modifiers));
        }

        return CollectionsMarshal.AsSpan(traits);
    }

    private TraitType GetTraitType(Child traitAttribute)
    {
        var traitType = TraitType.None;
        foreach (string traitTypeString in GetTraitTypes(traitAttribute))
        {
            traitType |= GetTraitType(traitTypeString);
        }

        return traitType;
    }

    private static List<string> GetTraitTypes(Child traitTypeAttribute)
    {
        var list = new List<string>(1);
        if (traitTypeAttribute.TryGetLeaf(out var leaf))
        {
            list.Add(leaf.ValueText);
        }

        if (traitTypeAttribute.TryGetNode(out var node))
        {
            list.AddRange(node.LeafValues.Select(trait => trait.ValueText));
        }

        return list;
    }

    private TraitType GetTraitType(string? traitType)
    {
        if (traitType is null)
        {
            return TraitType.None;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(traitType, "land"))
        {
            return TraitType.Land;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(traitType, "navy"))
        {
            return TraitType.Navy;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(traitType, "corps_commander"))
        {
            return TraitType.CorpsCommander;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(traitType, "field_marshal"))
        {
            return TraitType.FieldMarshal;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(traitType, "operative"))
        {
            return TraitType.Operative;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(traitType, "all"))
        {
            return TraitType.All;
        }

        Log.Warn("Unknown trait type: {TraitType}", traitType);
        return TraitType.None;
    }

    private static bool IsSkillModifier(Child traitAttribute, [NotNullWhen(true)] out Leaf? leaf)
    {
        bool isSkillModifier = traitAttribute.TryGetLeaf(out leaf);
        var traitLeaf = leaf;
        return isSkillModifier
            && (
                Array.Exists(
                    SkillModifierKeywords,
                    keyword => StringComparer.OrdinalIgnoreCase.Equals(keyword, traitLeaf?.Key)
                )
                || Array.Exists(
                    SkillFactorModifierKeywords,
                    keyword => StringComparer.OrdinalIgnoreCase.Equals(keyword, traitLeaf?.Key)
                )
            );
    }

    private static ModifierCollection ParseModifier(Node modifierNode)
    {
        var list = new List<IModifier>(modifierNode.AllArray.Length);
        foreach (var child in modifierNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                var modifier = LeafModifier.FromLeaf(leaf);
                list.Add(modifier);
            }
            else if (child.TryGetNode(out var node))
            {
                var modifier = new NodeModifier(node.Key, node.Leaves.Select(LeafModifier.FromLeaf));
                list.Add(modifier);
            }
        }

        return new ModifierCollection(modifierNode.Key, list);
    }
}
