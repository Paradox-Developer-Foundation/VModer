using System.Text;
using NLog;
using VModer.Core.Models;
using VModer.Core.Models.Character;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.Core.Services.GameResource.Modifiers;

public sealed class ModifierDisplayService
{
    public const string NodeModifierChildrenPrefix = "  ";

    private readonly LocalizationFormatService _localisationFormatService;
    private readonly LocalizationService _localizationService;
    private readonly ModifierService _modifierService;
    private readonly TerrainService _terrainService;
    private readonly LocalizationKeyMappingService _localisationKeyMappingService;
    private readonly CharacterSkillService _characterSkillService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ModifierDisplayService(
        LocalizationFormatService localisationFormatService,
        LocalizationService localizationService,
        ModifierService modifierService,
        LocalizationKeyMappingService localisationKeyMappingService,
        TerrainService terrainService,
        CharacterSkillService characterSkillService
    )
    {
        _localisationFormatService = localisationFormatService;
        _localizationService = localizationService;
        _modifierService = modifierService;
        _localisationKeyMappingService = localisationKeyMappingService;
        _terrainService = terrainService;
        _characterSkillService = characterSkillService;
    }

    public IEnumerable<string> GetSkillModifierDescription(
        SkillType skillType,
        SkillCharacterType skillCharacterType,
        ushort level
    )
    {
        var skillModifier = _characterSkillService
            .Skills.FirstOrDefault(skill => skill.SkillType == skillType)
            ?.GetModifierDescription(skillCharacterType, level);

        if (skillModifier is null || skillModifier.Modifiers.Count == 0)
        {
            return [];
        }

        return GetDescription(skillModifier.Modifiers);
    }

    /// <summary>
    /// 获取修饰符的描述, 每个元素对应一行
    /// </summary>
    /// <param name="modifiers"></param>
    /// <returns></returns>
    public IReadOnlyCollection<string> GetDescription(IEnumerable<IModifier> modifiers)
    {
        var inlines = new List<string>(8);

        foreach (var modifier in modifiers)
        {
            IEnumerable<string> addedInlines;
            switch (modifier.Type)
            {
                case ModifierType.Leaf:
                {
                    var leafModifier = (LeafModifier)modifier;
                    if (IsCustomToolTip(leafModifier.Key))
                    {
                        var sb = new StringBuilder();
                        string name = _localizationService.GetValue(leafModifier.Value);
                        foreach (var colorTextInfo in _localisationFormatService.GetFormatText(name))
                        {
                            sb.Append(colorTextInfo.DisplayText);
                        }
                        addedInlines = [sb.ToString()];
                    }
                    else
                    {
                        addedInlines = [GetDescriptionForLeaf(leafModifier)];
                    }

                    break;
                }
                case ModifierType.Node:
                {
                    var nodeModifier = (NodeModifier)modifier;
                    addedInlines = GetModifierDescriptionForNode(nodeModifier);
                    break;
                }
                default:
                    continue;
            }

            inlines.AddRange(addedInlines);
        }

        return inlines;
    }

    private static bool IsCustomToolTip(string modifierKey)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(modifierKey, LeafModifier.CustomEffectTooltipKey)
            || StringComparer.OrdinalIgnoreCase.Equals(modifierKey, LeafModifier.CustomModifierTooltipKey);
    }

    private string GetDescriptionForLeaf(LeafModifier modifier)
    {
        string modifierKey = _localisationKeyMappingService.TryGetValue(modifier.Key, out string? mappingKey)
            ? mappingKey
            : modifier.Key;
        string modifierName = GetModifierFormatTextFromText(modifierKey);
        string colon = modifierName.EndsWith(':') || modifierName.EndsWith('：') ? string.Empty : ": ";
        string number = modifier.Value;

        if (modifier.ValueType is GameValueType.Int or GameValueType.Float)
        {
            string modifierFormat = _modifierService.TryGetLocalizationFormat(modifierKey, out string? result)
                ? result
                : string.Empty;

            number = _modifierService.GetDisplayValue(modifier, modifierFormat);
        }

        return $"{modifierName}{colon}{number}";
    }

    /// <summary>
    /// 获取修饰符应用格式化后的本地化文本
    /// </summary>
    /// <param name="modifierKey"></param>
    /// <returns></returns>
    private string GetModifierFormatTextFromText(string modifierKey)
    {
        string modifierName = _modifierService.GetLocalizationName(modifierKey);

        var sb = new StringBuilder();
        foreach (var textInfo in _localisationFormatService.GetFormatText(modifierName))
        {
            sb.Append(textInfo.DisplayText);
        }
        return sb.ToString();
    }

    private List<string> GetModifierDescriptionForNode(NodeModifier nodeModifier)
    {
        if (_terrainService.Contains(nodeModifier.Key))
        {
            return GetTerrainModifierDescription(nodeModifier);
        }

        if (nodeModifier.Key.Equals("targeted_modifier", StringComparison.OrdinalIgnoreCase))
        {
            return GetTargetedModifierDescription(nodeModifier);
        }

        return GetDescriptionForUnknownNode(nodeModifier);
    }

    private List<string> GetTargetedModifierDescription(NodeModifier nodeModifier)
    {
        var descriptions = new List<string>();
        string? countryTag = nodeModifier
            .Modifiers.FirstOrDefault(modifier =>
                modifier.Key.Equals("tag", StringComparison.OrdinalIgnoreCase)
            )
            ?.Value;

        descriptions.Add(
            countryTag is null ? "缺失目标国家:" : $"对 {_localizationService.GetCountryNameByTag(countryTag)}:"
        );
        foreach (
            var modifier in nodeModifier.Modifiers.Where(modifier =>
                !modifier.Key.Equals("tag", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            string description;
            // generate_wargoal_tension_against 在 targeted_modifier 中的本地化键不一样
            if (modifier.Key.Equals("generate_wargoal_tension_against", StringComparison.OrdinalIgnoreCase))
            {
                description = GetDescriptionForLeaf(
                    new LeafModifier(
                        "MODIFIER_GENERATE_WARGOAL_TENSION_LIMIT_AGAINST_COUNTRY",
                        modifier.Value,
                        modifier.ValueType
                    )
                );
            }
            else
            {
                description = GetDescriptionForLeaf(modifier);
            }
            descriptions.Add($"{NodeModifierChildrenPrefix}{description}");
        }
        return descriptions;
    }

    /// <summary>
    /// 获取地形修饰符的描述
    /// </summary>
    /// <param name="nodeModifier"></param>
    /// <returns></returns>
    private List<string> GetTerrainModifierDescription(NodeModifier nodeModifier)
    {
        return GetDescriptionForNode(
            nodeModifier,
            leafModifier =>
            {
                string modifierName = _localizationService.GetValue($"STAT_ADJUSTER_{leafModifier.Key}");
                string modifierFormat = _localizationService.GetValue(
                    $"STAT_ADJUSTER_{leafModifier.Key}_DIFF"
                );
                return $"{NodeModifierChildrenPrefix}{modifierName}{_modifierService.GetDisplayValue(leafModifier, modifierFormat)}";
            }
        );
    }

    private List<string> GetDescriptionForUnknownNode(NodeModifier nodeModifier)
    {
        Log.Info("未知的节点修饰符: {Name}", nodeModifier.Key);
        return GetDescriptionForNode(
            nodeModifier,
            leafModifier => $"{NodeModifierChildrenPrefix}{GetDescriptionForLeaf(leafModifier)}"
        );
    }

    private List<string> GetDescriptionForNode(
        NodeModifier nodeModifier,
        Func<LeafModifier, string> converter
    )
    {
        var list = new List<string>(nodeModifier.Modifiers.Count * 2)
        {
            $"{_localizationService.GetValue(nodeModifier.Key)}:"
        };

        list.AddRange(nodeModifier.Modifiers.Select(converter));

        return list;
    }
}
