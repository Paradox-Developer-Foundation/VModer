using System.Diagnostics.CodeAnalysis;

namespace VModer.Core.Services.GameResource.Localization;

public interface ILocalizationService
{
    public string GetCountryNameByTag(string tag);
    public string GetValue(string key);
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value);
    public bool TryGetValueInAll(string key, [NotNullWhen(true)] out string? value);
}