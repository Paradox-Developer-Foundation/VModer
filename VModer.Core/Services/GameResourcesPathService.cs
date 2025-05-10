using NLog;
using VModer.Core.Extensions;

namespace VModer.Core.Services;

public sealed class GameResourcesPathService(
    SettingsService settingService,
    GameModDescriptorService descriptor
)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public FileOrigin GetFileOrigin(string filePath)
    {
        if (filePath.Contains(settingService.ModRootFolderPath))
        {
            return FileOrigin.Mod;
        }

        if (filePath.Contains(settingService.GameRootFolderPath))
        {
            return FileOrigin.Game;
        }
        return FileOrigin.Unknown;
    }

    /// <summary>
    /// 获得所有应该加载的文件绝对路径, Mod优先, 遵循 replace_path 指令
    /// </summary>
    /// <param name="folderRelativePath"></param>
    /// <param name="filter"></param>
    /// <param name="searchOption"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public IReadOnlyCollection<string> GetAllFilePriorModByRelativePathForFolder(
        string folderRelativePath,
        string filter,
        SearchOption searchOption
    )
    {
        Log.Info("正在获取文件夹 {Path} 下的文件", folderRelativePath);
        string modFolder = Path.Combine(settingService.ModRootFolderPath, folderRelativePath);
        string gameFolder = Path.Combine(settingService.GameRootFolderPath, folderRelativePath);

        if (!Directory.Exists(gameFolder))
        {
            throw new DirectoryNotFoundException($"找不到文件夹 {gameFolder}");
        }

        if (!Directory.Exists(modFolder))
        {
            return Directory.GetFiles(gameFolder, filter, searchOption);
        }

        if (descriptor.ReplacePaths.Contains(folderRelativePath))
        {
            Log.Debug(
                "MOD文件夹已完全替换游戏文件夹: \n\t {GamePath} => {ModPath}",
                gameFolder.ToFilePath(),
                modFolder.ToFilePath()
            );
            return Directory.GetFiles(modFolder, filter, searchOption);
        }

        string[] gameFilesPath = Directory.GetFiles(gameFolder, filter, searchOption);
        string[] modFilesPath = Directory.GetFiles(modFolder, filter, searchOption);
        return RemoveFileOfEqualName(gameFilesPath, modFilesPath);
    }

    /// <summary>
    /// 移除重名文件, 优先移除游戏本体文件
    /// </summary>
    /// <param name="gameFilePaths"></param>
    /// <param name="modFilePaths"></param>
    /// <returns>不重名的文件路径</returns>
    /// <exception cref="ArgumentException"></exception>
    private static IReadOnlyCollection<string> RemoveFileOfEqualName(
        string[] gameFilePaths,
        string[] modFilePaths
    )
    {
        var set = new Dictionary<string, string>(Math.Max(gameFilePaths.Length, modFilePaths.Length));

        // 优先读取Mod文件
        // TODO: 做一下性能测试, 看和原来的算法有什么区别
        foreach (string filePath in modFilePaths.Concat(gameFilePaths))
        {
            string fileName =
                Path.GetFileName(filePath) ?? throw new ArgumentException($"无法得到文件名: {filePath}");
            set.TryAdd(fileName, filePath);
        }

        return set.Values;
    }

    /// <summary>
    /// 根据相对路径获得游戏或者Mod文件的绝对路径, 优先Mod
    /// </summary>
    /// <remarks>
    /// 注意: 此方法会忽略mod描述文件中的 replace_path 指令
    /// </remarks>
    /// <param name="fileRelativePath">根目录下的相对路径</param>
    /// <exception cref="FileNotFoundException">游戏和mod中均不存在</exception>
    /// <returns>文件绝对路径</returns>
    public string GetFilePathPriorModByRelativePath(string fileRelativePath)
    {
        string modFilePath = Path.Combine(settingService.ModRootFolderPath, fileRelativePath);
        if (File.Exists(modFilePath))
        {
            return modFilePath;
        }

        string gameFilePath = Path.Combine(settingService.GameRootFolderPath, fileRelativePath);
        if (File.Exists(gameFilePath))
        {
            return gameFilePath;
        }

        throw new FileNotFoundException($"在Mod和游戏中均找不到目标文件 '{fileRelativePath}'");
    }
}
