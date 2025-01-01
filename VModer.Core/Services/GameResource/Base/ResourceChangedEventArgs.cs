namespace VModer.Core.Services.GameResource.Base;

public sealed class ResourceChangedEventArgs(string filePath) : EventArgs
{
    public string FilePath { get; } = filePath;
}