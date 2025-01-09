using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using EnumsNET;
using VModer.Core.Models;
using VModer.Core.Models.Character;

namespace VModer.Core.Services.GameResource.Localization;

/// <summary>
/// 用来解决脚本关键字与本地化文本中的键不一致的问题
/// </summary>
public sealed class LocalizationKeyMappingService
{
    /// <summary>
    /// 当调用方法查找Key对应的本地化文本时,如果字典内存在Key, 则使用Key对应的Value进行查询
    /// </summary>
    private readonly Dictionary<string, string> _localisationKeyMapping =
        new(16, StringComparer.OrdinalIgnoreCase);

    private const string FileName = "ModiferLocalizationKeyMapping.csv";

    public LocalizationKeyMappingService()
    {
        // TODO: 资源文件仅保留一份, 而不是三端都各有一份
        // 方便贡献, 冲突时可以处理, 因此不应该是二进制(可以生成二进制缓存文件, 第一次启动时生成二进制文件, 并记录Hash, 当更新时重新生成)
        string localizationKeyMappingFilePath = Path.Combine([App.AssetsFolder, FileName]);
        using var csv = new CsvReader(
            File.OpenText(localizationKeyMappingFilePath),
            CultureInfo.InvariantCulture
        );
        foreach (var info in csv.GetRecords<LocalizationKeyMappingInfo>())
        {
            if (string.IsNullOrWhiteSpace(info.RawKey) || string.IsNullOrWhiteSpace(info.MappingKey))
            {
#if DEBUG
                throw new ArgumentException("csv中有值为空");
#endif
                continue;
            }

            AddKeyMapping(info.RawKey.Trim(), info.MappingKey.Trim());
        }

        // 添加特性中技能的本地化映射
        // 6种技能类型, attack, defense, planning, logistics, maneuvering, coordination
        foreach (
            string name in SkillType.List.Where(skillType =>
                !skillType.Value.Equals("level", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            AddKeyMapping($"{name}_skill", $"trait_bonus_{name}");

            AddKeyMapping(
                $"{name}_skill_factor",
                // FACTOR 中是 Defence, 技能加成中就是 Defense, 不理解为什么要这样写
                name == "Defense"
                    ? "BOOST_DEFENCE_FACTOR"
                    : $"boost_{name}_factor"
            );
        }
    }

    /// <summary>
    /// 添加映射
    /// </summary>
    /// <param name="rawKey">原始键</param>
    /// <param name="mappingKey">映射键</param>
    private void AddKeyMapping(string rawKey, string mappingKey)
    {
        _localisationKeyMapping[rawKey] = mappingKey;
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? mappingKey)
    {
        return _localisationKeyMapping.TryGetValue(key, out mappingKey);
    }
}
