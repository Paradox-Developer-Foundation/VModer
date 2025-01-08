using CsvHelper.Configuration.Attributes;

namespace VModer.Core.Models;

public sealed class ModifierLocalizationFormatInfo
{
    public string Key { get; set; } = string.Empty;

    [Name("Format Info")]
    public string FormatInfo { get; set; } = string.Empty;
}
