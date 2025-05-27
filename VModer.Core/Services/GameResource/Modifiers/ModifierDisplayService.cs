using NLog;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Localization;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Services.GameResource.Modifiers;

public sealed class ModifierDisplayService
{
    public const string NodeModifierChildrenPrefix = "  ";

    private readonly LocalizationFormatService _localisationFormatService;
    private readonly ILocalizationService _localizationService;
    private readonly ModifierService _modifierService;
    private readonly TerrainService _terrainService;
    private readonly LocalizationKeyMappingService _localisationKeyMappingService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ModifierDisplayService(
        LocalizationFormatService localisationFormatService,
        ILocalizationService localizationService,
        ModifierService modifierService,
        LocalizationKeyMappingService localisationKeyMappingService,
        TerrainService terrainService
    )
    {
        _localisationFormatService = localisationFormatService;
        _localizationService = localizationService;
        _modifierService = modifierService;
        _localisationKeyMappingService = localisationKeyMappingService;
        _terrainService = terrainService;
    }

    /// <summary>
    /// 获取修饰符的描述, 每个元素对应一行
    /// </summary>
    /// <param name="modifiers"></param>
    /// <returns></returns>
    public IReadOnlyCollection<string> GetDescription(IEnumerable<IModifier> modifiers)
    {
        var descriptions = new List<string>(8);

        foreach (var modifier in modifiers)
        {
            descriptions.AddRange(GetDescription(modifier));
        }

        return descriptions;
    }

    /// <summary>
    /// 获取修饰符的描述, 每个元素对应一行
    /// </summary>
    /// <param name="modifier"></param>
    /// <returns></returns>
    private IEnumerable<string> GetDescription(IModifier modifier)
    {
        IEnumerable<string> addedInlines;
        switch (modifier.Type)
        {
            case ModifierType.Leaf:
            {
                var leafModifier = (LeafModifier)modifier;
                addedInlines = [GetDescription(leafModifier)];
                break;
            }
            case ModifierType.Node:
            {
                var nodeModifier = (NodeModifier)modifier;
                addedInlines = GetDescription(nodeModifier);
                break;
            }
            default:
                addedInlines = [];
                break;
        }

        return addedInlines;
    }

    public string GetDescription(LeafModifier modifier)
    {
        if (IsCustomToolTip(modifier.Key))
        {
            return _localisationFormatService.GetFormatText(modifier.Value);
        }

        return GetDescriptionForLeaf(modifier);
    }

    public IEnumerable<string> GetDescription(NodeModifier modifier)
    {
        return GetModifierDescriptionForNode(modifier);
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
        string modifierName = _localisationFormatService
            .GetFormatTextInfo(_modifierService.GetLocalizationName(modifierKey))
            .AsValueEnumerable()
            .Select(x => x.DisplayText)
            .JoinToString(string.Empty);

        string colon =
            modifierName.EndsWith(':')
            || modifierName.EndsWith('：')
            || modifierName.EndsWith("： ")
            || modifierName.EndsWith(": ")
                ? string.Empty
                : ": ";
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

    private IEnumerable<string> GetModifierDescriptionForNode(NodeModifier nodeModifier)
    {
        if (_terrainService.Contains(nodeModifier.Key))
        {
            return GetTerrainModifierDescription(nodeModifier);
        }

        if (nodeModifier.Key.Equals("targeted_modifier", StringComparison.OrdinalIgnoreCase))
        {
            return GetTargetedModifierDescription(nodeModifier);
        }

        if (nodeModifier.Key.Equals("equipment_bonus", StringComparison.OrdinalIgnoreCase))
        {
            return GetEquipmentModifierDescription(nodeModifier);
        }

        if (nodeModifier.Key.Equals(Keywords.HiddenModifier, StringComparison.OrdinalIgnoreCase))
        {
            // 直接展开 hidden_modifier 节点下的修饰符
            return nodeModifier.Leaves.Select(GetDescriptionForLeaf);
        }

        return GetDescriptionForUnknownNode(nodeModifier);
    }

    private List<string> GetEquipmentModifierDescription(NodeModifier nodeModifier)
    {
        var descriptions = new List<string>();
        foreach (
            var equipmentModifierNode in nodeModifier.Modifiers.AsValueEnumerable().OfType<NodeModifier>()
        )
        {
            descriptions.Add($"{_localisationFormatService.GetFormatText(equipmentModifierNode.Key)}:");
            foreach (var modifier in equipmentModifierNode.Leaves)
            {
                descriptions.Add($"{NodeModifierChildrenPrefix}{GetDescriptionForLeaf(modifier)}");
            }
        }
        return descriptions;
    }

    private List<string> GetTargetedModifierDescription(NodeModifier nodeModifier)
    {
        var descriptions = new List<string>();
        var countryTagLeaf = (LeafModifier?)
            nodeModifier
                .Modifiers.AsValueEnumerable()
                .FirstOrDefault(modifier =>
                    modifier.Type == ModifierType.Leaf
                    && modifier.Key.Equals("tag", StringComparison.OrdinalIgnoreCase)
                );

        string? countryTag = countryTagLeaf?.Value;
        descriptions.Add(
            countryTag is null
                ? Resources.MissingTargetCountry
                : $"{_localizationService.GetCountryNameByTag(countryTag)}:"
        );
        foreach (
            var modifier in nodeModifier
                .Modifiers.AsValueEnumerable()
                .Where(modifier =>
                    !modifier.Key.Equals("tag", StringComparison.OrdinalIgnoreCase)
                    && modifier.Type == ModifierType.Leaf
                )
                .Cast<LeafModifier>()
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
            nodeModifier.Key,
            // 地形修饰符中仅有 Leaf 类型, 但以防万一
            nodeModifier.Modifiers.Where(modifier => modifier.Type == ModifierType.Leaf),
            modifier =>
            {
                string modifierName = _localisationFormatService.GetFormatText(
                    $"STAT_ADJUSTER_{modifier.Key}"
                );
                string modifierFormat = _localizationService.GetValue($"STAT_ADJUSTER_{modifier.Key}_DIFF");
                return $"{NodeModifierChildrenPrefix}{modifierName}{_modifierService.GetDisplayValue((LeafModifier)modifier, modifierFormat)}";
            }
        );
    }

    private List<string> GetDescriptionForUnknownNode(NodeModifier nodeModifier)
    {
        Log.Debug("节点修饰符: {Name}", nodeModifier.Key);
        return GetDescriptionForNode(
            nodeModifier.Key,
            // 仅处理 Leaf, 省略 Node
            nodeModifier.Modifiers.Where(modifier => modifier.Type == ModifierType.Leaf),
            modifier => $"{NodeModifierChildrenPrefix}{GetDescriptionForLeaf((LeafModifier)modifier)}"
        );
    }

    private List<string> GetDescriptionForNode(
        string key,
        IEnumerable<IModifier> modifiers,
        Func<IModifier, string> converter
    )
    {
        var list = new List<string> { $"{_localisationFormatService.GetFormatText(key)}:" };

        list.AddRange(modifiers.Select(converter));

        return list;
    }
}
