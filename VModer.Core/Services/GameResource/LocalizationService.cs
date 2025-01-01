using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ParadoxPower.CSharp;
using ParadoxPower.Localisation;
using VModer.Core.Extensions;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class LocalizationService
    : ResourcesService<LocalizationService, FrozenDictionary<string, string>, YAMLLocalisationParser.LocFile>
{
    private Dictionary<string, FrozenDictionary<string, string>>.ValueCollection Localisations =>
        Resources.Values;

    // [Time("加载本地化文件")]
    public LocalizationService()
        : base(
            Path.Combine(
                "localisation",
                App.Services.GetRequiredService<SettingsService>().GameLanguage.ToGameLocalizationLanguage()
            ),
            WatcherFilter.LocalizationFiles,
            PathType.Folder
        ) { }

    /// <summary>
    /// 如果本地化文本不存在, 则返回<c>key</c>
    /// </summary>
    /// <returns></returns>
    public string GetValue(string key)
    {
        return TryGetValue(key, out var value) ? value : key;
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        foreach (var localisation in Localisations)
        {
            if (localisation.TryGetValue(key, out var result))
            {
                value = result;
                return true;
            }
        }

        value = null;
        return false;
    }

    protected override FrozenDictionary<string, string> ParseFileToContent(
        YAMLLocalisationParser.LocFile result
    )
    {
        var localisations = new Dictionary<string, string>(result.entries.Length);
        foreach (var item in result.entries)
        {
            localisations[item.key] = GetCleanDesc(item.desc);
        }

        return localisations.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// 去除开头和结尾的 "
    private static string GetCleanDesc(string rawDesc)
    {
        return rawDesc.Length switch
        {
            > 2 => rawDesc[1..^1],
            2 => string.Empty,
            _ => rawDesc
        };
    }

    protected override YAMLLocalisationParser.LocFile? GetParseResult(string filePath)
    {
        var localisation = YAMLLocalisationParser.parseLocFile(filePath);
        if (localisation.IsFailure)
        {
            Log.LogParseError(localisation.GetError());
            return null;
        }

        var result = localisation.GetResult();
        return result;
    }
}
