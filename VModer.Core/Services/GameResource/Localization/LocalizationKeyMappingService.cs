using System.Diagnostics.CodeAnalysis;
using EnumsNET;
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
        new(StringComparer.OrdinalIgnoreCase);

    public LocalizationKeyMappingService()
    {
        // 添加特性中技能的本地化映射
        // 6种技能类型, attack, defense, planning, logistics, maneuvering, coordination
        foreach (
            var skillType in Enums
                .GetNames<SkillType>()
                .Where(name => !name.Equals("level", StringComparison.OrdinalIgnoreCase))
        )
        {
            AddKeyMapping($"{skillType}_skill", $"trait_bonus_{skillType}");

            AddKeyMapping(
                $"{skillType}_skill_factor",
                // FACTOR 中是 Defence, 技能加成中就是 Defense, 不理解为什么要这样写
                skillType == "Defense"
                    ? "BOOST_DEFENCE_FACTOR"
                    : $"boost_{skillType}_factor"
            );
        }

        // 突破
        AddKeyMapping("breakthrough_factor", "MODIFIER_BREAKTHROUGH");
        // 对岸炮击加成
        AddKeyMapping("shore_bombardment_bonus", "MODIFIER_SHORE_BOMBARDMENT");
        // 每月人口
        AddKeyMapping("monthly_population", "MODIFIER_GLOBAL_MONTHLY_POPULATION");
        // 适役人口
        AddKeyMapping("conscription", "MODIFIER_CONSCRIPTION_FACTOR");
        // TODO: 数据库需要的信息
        // 1. 原始Key, 对应Key, 格式信息(可选), 效果类型(Good, Bad), 备注(可选)
        // 支持 "targeted_modifier"
    }

    /// <summary>
    /// 添加映射
    /// </summary>
    /// <param name="rawKey">原始键</param>
    /// <param name="mappingKey">映射键</param>
    public void AddKeyMapping(string rawKey, string mappingKey)
    {
        _localisationKeyMapping[rawKey] = mappingKey;
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? mappingKey)
    {
        return _localisationKeyMapping.TryGetValue(key, out mappingKey);
    }
}
