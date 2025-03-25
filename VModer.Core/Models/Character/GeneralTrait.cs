using ParadoxPower.Utilities;
using VModer.Core.Models.Modifiers;

namespace VModer.Core.Models.Character;

/// <summary>
/// 除去领导人之外的特质
/// </summary>
public sealed class GeneralTrait
{
    public const string TraitSkillModifiersKey = "skill_modifiers_key";
    public const string TraitXpFactor = "trait_xp_factor";

    public string Name { get; }
    public TraitType Type { get; }
    public IEnumerable<IModifier> AllModifiers =>
        ModifiersCollection
            .Where(collection => !collection.Key.Equals(TraitXpFactor, StringComparison.OrdinalIgnoreCase))
            .SelectMany(collection => collection.Modifiers);
    public ModifierCollection[] ModifiersCollection { get; }
    public Position.Range Position { get; }

    public GeneralTrait(
        string name,
        TraitType type,
        IEnumerable<ModifierCollection> modifiers,
        Position.Range range
    )
    {
        Name = name;
        Type = type;
        ModifiersCollection = modifiers.ToArray();
        Position = range;
    }
}
