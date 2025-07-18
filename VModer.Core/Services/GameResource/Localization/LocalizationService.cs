using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using MethodTimer;
using Microsoft.Extensions.DependencyInjection;
using ParadoxPower.CSharp;
using ParadoxPower.Localisation;
using VModer.Core.Extensions;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource.Localization;

public sealed class LocalizationService
    : ResourcesService<LocalizationService, FrozenDictionary<string, string>, YAMLLocalisationParser.LocFile>,
        ILocalizationService
{
    private ICollection<FrozenDictionary<string, string>> Localisations => Resources.Values;
    private readonly LocalizationKeyMappingService _localizationKeyMapping;

    [Time("加载本地化文件")]
    public LocalizationService(LocalizationKeyMappingService localizationKeyMapping)
        : base(
            Path.Combine(
                "localisation",
                App.Services.GetRequiredService<SettingsService>().GameLanguage.ToGameLocalizationLanguage()
            ),
            WatcherFilter.LocalizationFiles,
            PathType.Folder,
            SearchOption.AllDirectories,
            true
        )
    {
        _localizationKeyMapping = localizationKeyMapping;
    }

    public string GetCountryNameByTag(string tag)
    {
        if (TryGetValue(tag, out string? countryName))
        {
            return countryName;
        }

        if (TryGetValue($"{tag}_DEF", out countryName))
        {
            return countryName;
        }

        return tag;
    }

    /// <summary>
    /// 如果本地化文本不存在, 则返回<c>key</c>
    /// </summary>
    /// <returns></returns>
    public string GetValue(string key)
    {
        return TryGetValue(key, out string? value) ? value : key;
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        foreach (var localisation in Localisations)
        {
            if (localisation.TryGetValue(key, out string? result))
            {
                value = result;
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// 查找本地化字符串, 先尝试在 <see cref="LocalizationKeyMappingService"/> 中查找 Key 是否有替换的 Key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValueInAll(string key, [NotNullWhen(true)] out string? value)
    {
        if (_localizationKeyMapping.TryGetValue(key, out string? mappingKey))
        {
            key = mappingKey;
        }

        return TryGetValue(key, out value);
    }

    protected override FrozenDictionary<string, string> ParseFileToContent(
        YAMLLocalisationParser.LocFile result
    )
    {
        var localisations = new Dictionary<string, string>(result.Entries.Count);
        foreach (var item in result.Entries)
        {
            localisations[item.Key] = item.Desc;
        }

        return localisations.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    protected override YAMLLocalisationParser.LocFile? GetParseResult(string filePath)
    {
        var localisation = YAMLLocalisationParser.ParseLocFile(filePath);
        if (localisation.IsFailure)
        {
            Log.LogParseError(localisation.GetError()!);
            return null;
        }

        var result = localisation.GetResult()!;
        return result;
    }
}
