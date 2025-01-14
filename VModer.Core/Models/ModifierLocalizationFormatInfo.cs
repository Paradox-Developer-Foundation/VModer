using CsvHelper.Configuration.Attributes;

namespace VModer.Core.Models;

public sealed class ModifierLocalizationFormatInfo(
    [Name("Key")] string key,
    [Name("Format Info")] string formatInfo
)
{
    public string Key { get; } = key;

    public string FormatInfo { get; } = formatInfo;
}
