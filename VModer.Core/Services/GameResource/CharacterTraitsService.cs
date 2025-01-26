using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using MethodTimer;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Models.Character;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Base;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.Core.Services.GameResource;

public sealed class CharacterTraitsService
    : CommonResourcesService<CharacterTraitsService, FrozenDictionary<string, CharacterTrait>>
{
    public IEnumerable<CharacterTrait> GetAllTraits() => _allTraitsLazy.Value;

    private Lazy<IEnumerable<CharacterTrait>> _allTraitsLazy;
    private readonly LocalizationService _localizationService;
    private Dictionary<string, FrozenDictionary<string, CharacterTrait>>.ValueCollection Traits =>
        Resources.Values;

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

    [Time("加载人物特质")]
    public CharacterTraitsService(LocalizationService localizationService)
        : base(Path.Combine(Keywords.Common, "unit_leader"), WatcherFilter.Text)
    {
        _localizationService = localizationService;

        _allTraitsLazy = GetAllTraitsLazy();
        OnResourceChanged += (_, _) => _allTraitsLazy = GetAllTraitsLazy();
    }

    private Lazy<IEnumerable<CharacterTrait>> GetAllTraitsLazy() =>
        new(() => Traits.SelectMany(trait => trait.Values).ToArray());

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

    public string GetLocalizationName(CharacterTrait characterTrait)
    {
        return _localizationService.GetValue(characterTrait.Name);
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
