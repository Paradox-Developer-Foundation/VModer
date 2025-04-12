using System.Diagnostics.CodeAnalysis;
using VModer.Core.Models;

namespace VModer.Core.Services.GameResource.Localization;

public interface ILocalizationTextColorsService
{
    public bool TryGetColor(char key, [NotNullWhen(true)] out LocalizationTextColor? color);
}