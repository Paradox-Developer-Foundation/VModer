namespace VModer.Core.Models;

public sealed class SpriteInfo(string name, string relativePath, short totalFrames)
{
    public string Name { get; } = name;
    /// <summary>
    /// 图片的相对路径
    /// </summary>
    public string RelativePath { get; } = relativePath;
    /// <summary>
    /// 图片的帧数
    /// </summary>
    public short TotalFrames { get; } = totalFrames;
}