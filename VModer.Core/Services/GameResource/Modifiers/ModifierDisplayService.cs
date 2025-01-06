using System.Text;
using FParsec;
using Markdig.Helpers;
using NLog;
using VModer.Core.Models;
using VModer.Core.Models.Character;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.Core.Services.GameResource.Modifiers;

public sealed class ModifierDisplayService
{
    private readonly LocalizationFormatService _localisationFormatService;
    private readonly LocalizationService _localizationService;
    private readonly ModifierService _modifierService;
    private readonly TerrainService _terrainService;
    private readonly LocalizationKeyMappingService _localisationKeyMappingService;
    private readonly CharacterSkillService _characterSkillService;

    private const string NodeModifierChildrenPrefix = "  ";
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
            return ["None"];
        }

        return GetDescription(skillModifier.Modifiers);
    }

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
                        foreach (var colorTextInfo in _localisationFormatService.GetColorText(name))
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
        string modifierName = GetModifierColorTextFromText(modifierKey);

        if (modifier.ValueType is GameValueType.Int or GameValueType.Float)
        {
            string modifierFormat = _modifierService.TryGetLocalizationFormat(modifierKey, out string? result)
                ? result
                : string.Empty;
            return $"{modifierName}: {_modifierService.GetDisplayValue(modifier, modifierFormat)}";
        }

        return $"{modifierName}: {modifier.Value}";
    }

    private string GetModifierColorTextFromText(string modifierKey)
    {
        string modifierName = _modifierService.GetLocalizationName(modifierKey);

        var sb = new StringBuilder();
        foreach (var colorTextInfo in _localisationFormatService.GetColorText(modifierName))
        {
            sb.Append(colorTextInfo.DisplayText);
        }
        return sb.ToString();
    }

    private List<string> GetModifierDescriptionForNode(NodeModifier nodeModifier)
    {
        if (_terrainService.Contains(nodeModifier.Key))
        {
            return GetTerrainModifierDescription(nodeModifier);
        }

        return GetDescriptionForUnknownNode(nodeModifier);
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
        Log.Warn("未知的节点修饰符: {Name}", nodeModifier.Key);
        return GetDescriptionForNode(
            nodeModifier,
            leafModifier => $"{NodeModifierChildrenPrefix}{GetDescriptionForLeaf(leafModifier)}"
        );
    }

    private List<string> GetDescriptionForNode(NodeModifier nodeModifier, Func<LeafModifier, string> func)
    {
        var list = new List<string>(nodeModifier.Modifiers.Count * 2)
        {
            $"{_localizationService.GetValue(nodeModifier.Key)}:"
        };

        list.AddRange(nodeModifier.Modifiers.Select(func));

        return list;
    }
}
