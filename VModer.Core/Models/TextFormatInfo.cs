using System.Drawing;

namespace VModer.Core.Models;

public sealed class TextFormatInfo(string text, Color color)
{
    public string DisplayText { get; } = text;
    public Color Color { get; } = color;
}
