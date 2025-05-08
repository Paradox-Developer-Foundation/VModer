using ParadoxPower.Utilities;
using VModer.Core.Models.Modifiers;

namespace VModer.Core.Models;

public sealed class LeaderTrait(string name, IEnumerable<IModifier> modifiers, Position.Range position)
{
    public string Name { get; } = name;
    public IEnumerable<IModifier> Modifiers { get; } = modifiers.ToArray();
    public Position.Range Position { get; } = position;
}
