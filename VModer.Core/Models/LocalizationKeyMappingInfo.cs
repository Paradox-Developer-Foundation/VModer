using CsvHelper.Configuration.Attributes;

namespace VModer.Core.Models;

public sealed class LocalizationKeyMappingInfo(
    [Name("Raw Key")] string rawKey,
    [Name("Mapping Key")] string mappingKey)
{
    public string RawKey { get; } = rawKey;

    public string MappingKey { get; } = mappingKey;
}
