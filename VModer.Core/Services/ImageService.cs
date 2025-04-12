using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NLog;
using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using VModer.Core.Services.GameResource;

namespace VModer.Core.Services;

public sealed class ImageService : IImageService
{
    private readonly SpriteService _spriteService;
    private readonly string _cachePath;
    private readonly GameResourcesPathService _pathService;

    /// <summary>
    /// 键为 spriteName, 当有 frame 参数时 spriteName^frame, 则为, 值为图片绝对路径
    /// </summary>
    private readonly Dictionary<string, string> _localImages = new();
    private const string CacheFolderPath = "local_image_cache";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ImageService(
        SettingsService settingsService,
        SpriteService spriteService,
        GameResourcesPathService pathService
    )
    {
        _spriteService = spriteService;
        _pathService = pathService;
        _cachePath = Path.Combine(settingsService.ExtensionPath, CacheFolderPath);
        Log.Info("本地图片缓存路径: {Path}", _cachePath);

        if (!Directory.Exists(_cachePath))
        {
            Directory.CreateDirectory(_cachePath);
            return;
        }

        uint count = 0;
        foreach (string filePath in Directory.EnumerateFiles(_cachePath))
        {
            ++count;
            _localImages.Add(Path.GetFileNameWithoutExtension(filePath), filePath);
        }

        Log.Info("本地图片缓存数量: {Count}", count);
    }

    public void ClearCache()
    {
        _localImages.Clear();
        foreach (string file in Directory.EnumerateFiles(_cachePath))
        {
            File.Delete(file);
        }

        // 删除所有子文件夹及其内容
        foreach (string subfolder in Directory.EnumerateDirectories(_cachePath))
        {
            Directory.Delete(subfolder, true);
        }
    }

    /// <summary>
    /// 尝试获取精灵的图片 Uri, 如果格式不支持, 则转化为 Png 后返回 Png 图片的Uri
    /// </summary>
    /// <param name="spriteName"></param>
    /// <param name="localImageUri"></param>
    /// <returns></returns>
    public bool TryGetLocalImagePathBySpriteName(
        string spriteName,
        [NotNullWhen(true)] out string? localImageUri
    )
    {
        if (TryGetLocalImagePathBySpriteName(spriteName, 1, out localImageUri))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试获取精灵的图片 Uri, 如果格式不支持, 则转化为 Png 后返回 Png 图片的Uri
    /// </summary>
    /// <param name="spriteName"></param>
    /// <param name="frame">需要的图片帧数, 不需要切割时应为 1</param>
    /// <param name="localImageUri"></param>
    /// <returns></returns>
    public bool TryGetLocalImagePathBySpriteName(
        string spriteName,
        short frame,
        [NotNullWhen(true)] out string? localImageUri
    )
    {
        if (_spriteService.TryGetSpriteInfo(spriteName, out var spriteInfo))
        {
            try
            {
                localImageUri = GetLocalImageUri(
                    spriteName,
                    _pathService.GetFilePathPriorModByRelativePath(spriteInfo.RelativePath),
                    spriteInfo.TotalFrames,
                    frame
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "获取本地图片路径失败: {Name}", spriteName);
                localImageUri = null;
                return false;
            }

            return true;
        }

        localImageUri = null;
        return false;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="imagePath">游戏图片资源绝对路径</param>
    /// <param name="totalFrames"></param>
    /// <param name="frame"></param>
    /// <param name="spriteName"></param>
    /// <exception cref="ArgumentException">图片转换失败, <c>totalFrames</c> 参数错误</exception>
    /// <returns>图片的本地Uri</returns>
    private string GetLocalImageUri(string spriteName, string imagePath, short totalFrames, short frame)
    {
        CheckFrame(totalFrames, frame);

        string queryKey =
            totalFrames == 1 ? spriteName : GetMultipleFrameImageFileNameWithoutExtension(spriteName, frame);
        if (_localImages.TryGetValue(queryKey, out string? localImagePath))
        {
            return new Uri(localImagePath).ToString();
        }

        Log.Debug("未在缓存中找到图片: {Name}", spriteName);

        Uri imageUri;
        var imageExtension = Path.GetExtension(imagePath.AsSpan());
        if (
            imageExtension.Equals(".dds", StringComparison.OrdinalIgnoreCase)
            || imageExtension.Equals(".tga", StringComparison.OrdinalIgnoreCase)
        )
        {
            string outputPath = ConvertToPng(spriteName, imagePath, totalFrames, frame);
            _localImages.Add(queryKey, outputPath);
            imageUri = new Uri(outputPath);

            Log.Debug("{RawName} 转换为 {Name}", Path.GetFileName(imagePath), Path.GetFileName(outputPath));
        }
        else if (
            imageExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || imageExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        )
        {
            imageUri = new Uri(imagePath);
        }
        else
        {
            return string.Empty;
        }

        return imageUri.ToString();
    }

    [Conditional("DEBUG")]
    private static void CheckFrame(int totalFrames, int frame)
    {
        Debug.Assert(totalFrames > 0 && frame > 0);
        if (totalFrames == 1)
        {
            Debug.Assert(frame == 1);
        }
    }

    private string ConvertToPng(string spriteName, string filePath, short totalFrames, short frame)
    {
        using var image = Pfimage.FromFile(filePath);
        byte[] newData;

        // Since image sharp can't handle data with line padding in a stride
        // we create an stripped down array if any padding is detected
        int tightStride = image.Width * image.BitsPerPixel / 8;
        if (image.Stride != tightStride)
        {
            newData = new byte[image.Height * tightStride];
            for (int i = 0; i < image.Height; i++)
            {
                Buffer.BlockCopy(image.Data, i * image.Stride, newData, i * tightStride, tightStride);
            }
        }
        else
        {
            newData = image.Data;
        }

        return SaveAsPng(spriteName, GetImageByFormat(image, newData), totalFrames, frame);
    }

    private string SaveAsPng(string spriteName, Image image, short totalFrames, short frame)
    {
        string outputPath;
        if (totalFrames == 1)
        {
            outputPath = GetSingleFrameImagePath(spriteName);
            image.SaveAsPng(outputPath);
        }
        else
        {
            outputPath = GetMultipleFrameImagePath(spriteName, frame);
            // 计算每帧宽度
            int frameWidth = image.Width / totalFrames;
            if (frameWidth * totalFrames != image.Width)
            {
                throw new ArgumentException("图像宽度必须能被总帧数整除，当前宽度: " + $"{image.Width}, 总帧数: {totalFrames}");
            }

            // 遍历每一帧
            for (int i = 0; i < totalFrames; i++)
            {
                // 计算裁剪区域
                int x = i * frameWidth;
                var cropRect = new Rectangle(x, 0, frameWidth, image.Height);

                // 裁剪并保存
                using var frameImage = image.Clone(ctx => ctx.Crop(cropRect));
                frameImage.SaveAsPng(GetMultipleFrameImagePath(spriteName, i));
            }
        }

        image.Dispose();
        return outputPath;
    }

    private static Image GetImageByFormat(IImage image, byte[] newData)
    {
        Image data;
        switch (image.Format)
        {
            case ImageFormat.Rgba32:
            {
                data = Image.LoadPixelData<Bgra32>(newData, image.Width, image.Height);
                break;
            }
            case ImageFormat.Rgb24:
            {
                data = Image.LoadPixelData<Bgr24>(newData, image.Width, image.Height);
                break;
            }
            case ImageFormat.Rgba16:
            {
                data = Image.LoadPixelData<Bgra4444>(newData, image.Width, image.Height);
                break;
            }
            case ImageFormat.R5g5b5:
            {
                // Turn the alpha channel on for image sharp.
                for (int i = 1; i < newData.Length; i += 2)
                {
                    newData[i] |= 128;
                }
                data = Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
                break;
            }
            case ImageFormat.R5g5b5a1:
            {
                data = Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
                break;
            }
            case ImageFormat.R5g6b5:
            {
                data = Image.LoadPixelData<Bgr565>(newData, image.Width, image.Height);
                break;
            }
            case ImageFormat.Rgb8:
            {
                data = Image.LoadPixelData<L8>(newData, image.Width, image.Height);
                break;
            }
            default:
                throw new Exception($"ImageSharp does not recognize image format: {image.Format}");
        }

        return data;
    }

    private string GetSingleFrameImagePath(string spriteName)
    {
        string outputPath = Path.Combine(_cachePath, $"{spriteName}.png");
        return outputPath;
    }

    private string GetMultipleFrameImagePath(string spriteName, int frame)
    {
        string outputPath = Path.Combine(
            _cachePath,
            $"{GetMultipleFrameImageFileNameWithoutExtension(spriteName, frame)}.png"
        );
        return outputPath;
    }

    /// <summary>
    /// 获取多帧图片文件名, 不包含扩展名
    /// </summary>
    /// <param name="spriteName"></param>
    /// <param name="frame"></param>
    /// <returns></returns>
    private static string GetMultipleFrameImageFileNameWithoutExtension(string spriteName, int frame)
    {
        return $"{spriteName}^{frame.ToString(CultureInfo.InvariantCulture)}";
    }
}
