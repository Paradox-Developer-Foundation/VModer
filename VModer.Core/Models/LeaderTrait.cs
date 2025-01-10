using VModer.Core.Models.Modifiers;

namespace VModer.Core.Models;

public sealed class LeaderTrait
{
    public LeaderTrait(string name, IEnumerable<IModifier> modifiers)
    {
        Name = name;
        Modifiers = modifiers.ToArray();
    }

    public string Name { get; }
    public IEnumerable<IModifier> Modifiers { get; }
}
