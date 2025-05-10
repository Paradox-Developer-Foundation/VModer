namespace VModer.Core.Models.Character;

/// <summary>
/// 存储某一项属性(攻击, 防御等)的每一级别的信息
/// </summary>
public sealed class SkillInfo(SkillType skillType)
{
    public SkillType SkillType { get; } = skillType;
    private readonly List<Skill> _skills = new(3);

    public void Add(Skill skill)
    {
        _skills.Add(skill);
    }

    public ushort? GetMaxValue(SkillCharacterType type)
    {
        return _skills.Find(skill => skill.Type == type)?.MaxValue;
    }

    public SkillModifier? GetModifierDescription(SkillCharacterType type, ushort level)
    {
        return _skills.Find(skill => skill.Type == type)?.GetModifier(level);
    }
}
