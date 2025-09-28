using System.Text.Json.Serialization;

namespace VModer.Core.Models;

[JsonSerializable(typeof(VictoryPoint))]
internal partial class VictoryPointContext : JsonSerializerContext;

public readonly struct VictoryPoint(int provinceId, int value)
{
    public int ProvinceId { get; init; } = provinceId;
    public int Value { get; init; } = value;
}
