namespace VModer.Core.Models;

public sealed class BuildingInfo(string name, int? maxLevel)
{
	public string Name { get; } = name;
	public int? MaxLevel { get; } = maxLevel;
}