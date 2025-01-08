using CsvHelper.Configuration.Attributes;

namespace VModer.Core.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class LocalizationKeyMappingInfo
{
    [Name("Raw Key")]
    public string RawKey { get; set; } = string.Empty;

    [Name("Mapping Key")]
    public string MappingKey { get; set; } = string.Empty;
}
