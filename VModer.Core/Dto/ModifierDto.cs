namespace VModer.Core.Dto;

public sealed class ModifierDto
{
    public required string Name { get; init; }
    public required string LocalizedName { get; init; }
    public required string[] Categories { get; init; }
}