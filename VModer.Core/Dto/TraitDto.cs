using System.Text.Json.Serialization;
using VModer.Core.Models.Character;
using VModer.Core.Services;

namespace VModer.Core.Dto;

public sealed class TraitDto
{
    public required string Name { get; init; }
    public required string LocalizedName { get; init; }
    public required string Modifiers { get; init; }
    public required FileOrigin FileOrigin { get; init; }
    public required TraitType Type { get; init; }

    /// <summary>
    /// 特质描述信息
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}
