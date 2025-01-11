using System.Collections.Frozen;
using NLog;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;

namespace VModer.Core.Services;

public sealed class GameModDescriptorService
{
    public string Name { get; } = string.Empty;

    /// <summary>
    /// 保存着替换的文件夹相对路径的只读集合
    /// </summary>
    /// <remarks>
    /// 线程安全
    /// </remarks>
    public IReadOnlySet<string> ReplacePaths => _replacePaths;
    private readonly FrozenSet<string> _replacePaths;

    private const string FileName = "descriptor.mod";

    /// <summary>
    /// 按文件绝对路径构建
    /// </summary>
    /// <exception cref="FileNotFoundException">当文件不存在时</exception>
    /// <exception cref="IOException"></exception>
    public GameModDescriptorService(SettingsService settingService)
    {
        var logger = LogManager.GetCurrentClassLogger();
        string descriptorFilePath = Path.Combine(settingService.ModRootFolderPath, FileName);
        if (!File.Exists(descriptorFilePath))
        {
            _replacePaths = FrozenSet<string>.Empty;
            logger.Warn("Mod 描述文件不存在");
            return;
        }

        if (!TextParser.TryParse(descriptorFilePath, out var rootNode, out var error))
        {
            _replacePaths = FrozenSet<string>.Empty;
            logger.Warn("Mod descriptor.mod file read is failure");
            logger.LogParseError(error);
            return;
        }

        var replacePathList = new List<string>();

        foreach (var item in rootNode.Leaves)
        {
            switch (item.Key)
            {
                case "name":
                    Name = item.ValueText;
                    break;
                case "replace_path":
                    string[] parts = item.ValueText.Split('/');
                    replacePathList.Add(Path.Combine(parts));
                    break;
            }
        }
        _replacePaths = replacePathList.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
