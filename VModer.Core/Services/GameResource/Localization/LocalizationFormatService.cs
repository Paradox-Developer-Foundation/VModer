using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;

namespace VModer.Core.Services.GameResource.Localization;

public sealed class LocalizationFormatService(
    LocalizationTextColorsService localizationTextColorsService,
    LocalizationService localizationService,
    ImageService imageService
)
{
    /// <summary>
    /// 根据 <c>key</c> 获取格式化后的文本
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>格式化后的文本, 如果找到值, 返回<c>true</c>, 反之返回<c>false</c></returns>
    public bool TryGetFormatText(string key, [NotNullWhen(true)] out string? value)
    {
        if (localizationService.TryGetValue(key, out value))
        {
            value = GetFormatTextByText(value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 根据 <c>key</c> 获取格式化后的文本
    /// </summary>
    /// <param name="key"></param>
    /// <returns>格式化后的文本, 如果未找到值, 则返回<c>key</c></returns>
    public string GetFormatText(string key)
    {
        if (TryGetFormatText(key, out string? value))
        {
            return value;
        }

        return key;
    }

    /// <summary>
    /// 获取格式化后的文本
    /// </summary>
    /// <param name="text"></param>
    /// <returns>一个格式化后被拼接的文本</returns>
    private string GetFormatTextByText(string text)
    {
        return string.Join(string.Empty, GetFormatTextInfo(text).Select(info => info.DisplayText));
    }

    /// <summary>
    /// 获取格式化后的文本信息, 不包含 Icon, 如果解析文本颜色失败, 则统一使用黑色
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>一个集合, 包含格式化后的文本</returns>
    /// <remarks>
    /// 现支持
    /// 1. 文本颜色格式
    /// 2. 对其他本地化键的引用
    /// 3. Icon 引用
    /// </remarks>
    public IReadOnlyCollection<TextFormatInfo> GetFormatTextInfo(string text)
    {
        var result = new List<TextFormatInfo>(4);

        ParseFormat(text, result);

        return result;
    }

    private void ParseFormatToList(IEnumerable<LocalizationFormatInfo> formats, List<TextFormatInfo> result)
    {
        foreach (var format in formats)
        {
            if (format.Type == LocalizationFormatType.Placeholder)
            {
                // 一般来说, 包含管道符或文本为 VALUE | VAL 的为格式说明字符串, 不需要处理
                if (format.Text.Contains('|') || format.Text == "VALUE" || format.Text == "VAL")
                {
                    continue;
                }

                // 递归处理所有本地化引用
                string text = localizationService.GetValue(format.Text);
                ParseFormat(text, result);
            }
            else if (format.Type == LocalizationFormatType.Icon)
            {
                ParseIcon(format, result);
            }
            else
            {
                result.Add(GetColorText(format));
            }
        }
    }

    private void ParseFormat(string text, List<TextFormatInfo> result)
    {
        if (LocalizationFormatParser.TryParse(text, out var formats))
        {
            ParseFormatToList(formats, result);
        }
        else
        {
            result.Add(new TextFormatInfo(text, Color.Black));
        }
    }

    private void ParseIcon(LocalizationFormatInfo format, List<TextFormatInfo> result)
    {
        int frame = 1;
        string[] frameStr = format.Text.Split('|');
        if (frameStr.Length > 1 && int.TryParse(frameStr[1], out int frameResult))
        {
            frame = Math.Max(1, frameResult);
        }
        // icon 都为此格式
        string spriteName = $"GFX_{frameStr[0]}";

        if (
            imageService.TryGetLocalImagePathBySpriteName(spriteName, (short)frame, out string? imagePath)
            && !string.IsNullOrEmpty(imagePath)
        )
        {
            result.Add(new TextFormatInfo($"![icon]({imagePath})", Color.Black));
        }
    }

    /// <summary>
    /// 尝试将文本解析为 <see cref="TextFormatInfo"/>, 并使用 <see cref="LocalizationFormatInfo"/> 中指定的颜色, 如果颜色不存在, 则使用默认颜色
    /// </summary>
    /// <param name="format">文本格式信息</param>
    /// <returns></returns>
    private TextFormatInfo GetColorText(LocalizationFormatInfo format)
    {
        if (format.Type == LocalizationFormatType.TextWithColor)
        {
            if (string.IsNullOrEmpty(format.Text))
            {
                return new TextFormatInfo(string.Empty, Color.Black);
            }

            if (localizationTextColorsService.TryGetColor(format.Text[0], out var colorInfo))
            {
                if (!_colors.TryGetValue(format.Text[0], out var brush))
                {
                    _colors.Add(format.Text[0], brush);
                }
                return new TextFormatInfo(format.Text[1..], brush);
            }
        }

        return new TextFormatInfo(format.Text, Color.Black);
    }

    private readonly Dictionary<char, Color> _colors = [];
}
