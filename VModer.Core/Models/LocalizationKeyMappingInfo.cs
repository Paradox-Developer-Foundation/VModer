using CsvHelper.Configuration.Attributes;

namespace VModer.Core.Models;

public sealed class LocalizationKeyMappingInfo
{
    [Name("Raw Key")]
    public string RawKey { get; set; } = string.Empty;

    [Name("Mapping Key")]
    public string MappingKey { get; set; } = string.Empty;
}
