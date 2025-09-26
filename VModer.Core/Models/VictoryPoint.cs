namespace VModer.Core.Models;

public readonly struct VictoryPoint(int provinceId, int value)
{
    public int ProvinceId { get; init; } = provinceId;
    public int Value { get; init; } = value;
}
