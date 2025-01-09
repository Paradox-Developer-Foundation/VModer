using Ardalis.SmartEnum;

namespace VModer.Core.Models.Character;

/// <summary>
/// 技能类型, 比如攻击, 防御等等
/// </summary>
public sealed class SkillType(string name, string value) : SmartEnum<SkillType, string>(name, value)
{
    public static readonly SkillType Level = new(nameof(Level), "level");
    public static readonly SkillType Attack = new(nameof(Attack), "attack_skill");
    public static readonly SkillType Defense = new(nameof(Defense), "defense_skill");
    public static readonly SkillType Planning = new(nameof(Planning), "planning_skill");
    public static readonly SkillType Logistics = new(nameof(Logistics), "logistics_skill");
    public static readonly SkillType Maneuvering = new(nameof(Maneuvering), "maneuvering_skill");
    public static readonly SkillType Coordination = new(nameof(Coordination), "coordination_skill");
}
