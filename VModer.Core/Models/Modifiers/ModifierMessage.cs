namespace VModer.Core.Models.Modifiers;

public sealed class ModifierMessage(string name, string[] categories)
{
    public string Name { get; } = name;
    public string[] Categories { get; } = categories;
}
