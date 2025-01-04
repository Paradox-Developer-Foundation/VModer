namespace VModer.Core.Models;

public sealed class BuildingInfo(string name, ushort? maxLevel)
{
	public string Name { get; } = name;
	public ushort? MaxLevel { get; } = maxLevel;
}