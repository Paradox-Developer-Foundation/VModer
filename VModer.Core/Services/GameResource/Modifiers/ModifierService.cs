using System.Diagnostics.CodeAnalysis;
using NLog;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.Core.Services.GameResource.Modifiers;

public sealed class ModifierService
{
    private readonly LocalizationService _localizationService;
    private readonly ModiferLocalizationFormatService _modifierLocalizationFormatService;

    public ModifierService(
        LocalizationService localizationService,
        ModiferLocalizationFormatService modifierLocalizationFormatService
    )
    {
        _localizationService = localizationService;
        _modifierLocalizationFormatService = modifierLocalizationFormatService;
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public bool TryGetLocalizationName(string modifierKey, [NotNullWhen(true)] out string? value)
    {
        if (_localizationService.TryGetValueInAll(modifierKey, out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIERS_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_NAVAL_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_UNIT_LEADER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_ARMY_LEADER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_{modifierKey}_LIMIT", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"{modifierKey}_tt", out value))
        {
            return true;
        }

        return false;
    }

    public string GetLocalizationName(string modifierKey)
    {
        if (TryGetLocalizationName(modifierKey, out string? value))
        {
            return value;
        }

        return modifierKey;
    }

    public bool TryGetLocalizationFormat(string modifier, [NotNullWhen(true)] out string? result)
    {
        if (_modifierLocalizationFormatService.TryGetLocalizationFormat(modifier, out result))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"{modifier}_tt", out result))
        {
            return true;
        }

        return _localizationService.TryGetValue(modifier, out result);
    }

    private static ModifierEffectType GetModifierType(string modifierName, string modifierFormat)
    {
        // TODO: 重新支持从数据库中定义修饰符
        // if (_modifierTypes.TryGetValue(modifierName, out var modifierType))
        // {
        //     return modifierType;
        // }

        for (var index = modifierFormat.Length - 1; index >= 0; index--)
        {
            var c = modifierFormat[index];
            switch (c)
            {
                case '+':
                    return ModifierEffectType.Positive;
                case '-':
                    return ModifierEffectType.Negative;
            }
        }

        return ModifierEffectType.Unknown;
    }

    /// <summary>
    /// 获取 Modifier 数值的显示值
    /// </summary>
    /// <param name="leafModifier">包含关键字和对应值的修饰符对象</param>
    /// <param name="modifierDisplayFormat">修饰符对应的格式化设置文本, 为空时使用百分比格式</param>
    /// <returns>应用<c>modifierDisplayFormat</c>格式的<c>LeafModifier.Value</c>的的显示值</returns>
    public string GetDisplayValue(LeafModifier leafModifier, string modifierDisplayFormat)
    {
        if (leafModifier.ValueType is GameValueType.Int or GameValueType.Float)
        {
            double value = double.Parse(leafModifier.Value);
            string sign = leafModifier.Value.StartsWith('-') ? string.Empty : "+";

            char displayDigits = GetDisplayDigits(modifierDisplayFormat);
            bool isPercentage =
                string.IsNullOrEmpty(modifierDisplayFormat)
                || leafModifier.Key.EndsWith("factor")
                || modifierDisplayFormat.Contains('%');
            char format = isPercentage ? 'P' : 'F';

            return $"{sign}{value.ToString($"{format}{displayDigits}")}";
        }

        return leafModifier.Value;
    }

    private static char GetDisplayDigits(string modifierDescription)
    {
        char displayDigits = '1';
        for (int i = modifierDescription.Length - 1; i >= 0; i--)
        {
            char c = modifierDescription[i];
            if (char.IsDigit(c))
            {
                displayDigits = c;
                break;
            }
        }

        return displayDigits;
    }
}
