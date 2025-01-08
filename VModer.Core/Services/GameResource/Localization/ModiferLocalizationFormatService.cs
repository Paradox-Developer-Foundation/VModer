using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CsvHelper;
using NLog;
using VModer.Core.Models;

namespace VModer.Core.Services.GameResource.Localization;

/// <summary>
/// 用来补全缺少的修饰符格式
/// </summary>
public sealed class ModiferLocalizationFormatService
{
    private readonly FrozenDictionary<string, string> _modifierLocalizationFormat;

    private const string FileName = "ModifierLocalizationFormatInfo.csv";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ModiferLocalizationFormatService()
    {
        string filePath = Path.Combine([App.AssetsFolder, FileName]);
        using var csv = new CsvReader(File.OpenText(filePath), CultureInfo.InvariantCulture);
        var formatMap = new Dictionary<string, string>(8);
        foreach (var record in csv.GetRecords<ModifierLocalizationFormatInfo>())
        {
            if (string.IsNullOrWhiteSpace(record.Key) || string.IsNullOrWhiteSpace(record.FormatInfo))
            {
#if DEBUG
                throw new ArgumentException("csv中有值为空");
#endif
                continue;
            }

            formatMap.Add(record.Key.Trim(), record.FormatInfo.Trim());
        }

        _modifierLocalizationFormat = formatMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetLocalizationFormat(string modifier, [NotNullWhen(true)] out string? formatInfo)
    {
        if (_modifierLocalizationFormat.TryGetValue(modifier, out formatInfo))
        {
            return true;
        }

        formatInfo = null;
        return false;
    }
}
