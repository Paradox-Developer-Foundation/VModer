using System.Diagnostics.CodeAnalysis;

namespace VModer.Core.Services;

public interface IImageService
{
    public void ClearCache();

    public bool TryGetLocalImagePathBySpriteName(
        string spriteName,
        [NotNullWhen(true)] out string? localImageUri
    );

    public bool TryGetLocalImagePathBySpriteName(
        string spriteName,
        short frame,
        [NotNullWhen(true)] out string? localImageUri
    );
}
