using VModer.Core.Models.Modifiers;

namespace VModer.Core.Models.Character;

/// <summary>
/// 人物特质
/// </summary>
public sealed class CharacterTrait
{
    public string Name { get; }
    public TraitType Type { get; }
    public IEnumerable<IModifier> AllModifiers =>
        Modifiers
            .Where(collection => !collection.Key.Equals(TraitXpFactor, StringComparison.OrdinalIgnoreCase))
            .SelectMany(collection => collection.Modifiers);

    public IEnumerable<IModifier> TraitXpModifiers =>
        Modifiers
            .Where(collection => collection.Key.Equals(TraitXpFactor, StringComparison.OrdinalIgnoreCase))
            .SelectMany(collection => collection.Modifiers);

    public const string TraitSkillModifiersKey = "skill_modifiers_key";
    public const string TraitXpFactor = "trait_xp_factor";

    private ModifierCollection[] Modifiers { get; }

    public CharacterTrait(string name, TraitType type, IEnumerable<ModifierCollection> modifiers)
    {
        Name = name;
        Type = type;
        Modifiers = modifiers.ToArray();
    }
}
