using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace VModer.Core.Services.GameResource.Base;

public abstract partial class ResourcesService<TType, TContent, TParseResult> : IResourcesService
    where TType : ResourcesService<TType, TContent, TParseResult>
{
    public event EventHandler<ResourceChangedEventArgs>? OnResourceChanged;

    /// <summary>
    /// key: 文件路径, value: 文件内资源内容
    /// </summary>
    protected readonly IDictionary<string, TContent> Resources;
    protected readonly Logger Log;

    private readonly SettingsService _settingService;
    private readonly string _serviceName = typeof(TType).Name;
    private readonly string _folderOrFileRelativePath;

    /// <summary>
    ///
    /// </summary>
    /// <param name="folderOrFileRelativePath">文件夹或文件的相对路径</param>
    /// <param name="filter">过滤条件</param>
    /// <param name="pathType"><c>folderOrFileRelativePath</c>的类型</param>
    /// <param name="searchOption">指定文件夹的搜索模式, 当路径是文件时此配置无效</param>
    /// <param name="isAsyncLoading">是否多线程加载资源, 子类需要确保重写的方法是线程安全的</param>
    protected ResourcesService(
        string folderOrFileRelativePath,
        WatcherFilter filter,
        PathType pathType,
        SearchOption searchOption = SearchOption.TopDirectoryOnly,
        bool isAsyncLoading = false
    )
    {
        _folderOrFileRelativePath = folderOrFileRelativePath;
        Log = LogManager.GetLogger(typeof(TType).FullName);
        _settingService = App.Services.GetRequiredService<SettingsService>();

        var gameResourcesPathService = App.Services.GetRequiredService<GameResourcesPathService>();
        var watcherService = App.Services.GetRequiredService<GameResourcesWatcherService>();

        bool isFolderPath = pathType == PathType.Folder;
        string[] filePaths = isFolderPath
            ? gameResourcesPathService
                .GetAllFilePriorModByRelativePathForFolder(
                    _folderOrFileRelativePath,
                    filter.Name,
                    searchOption
                )
                .ToArray()
            : [gameResourcesPathService.GetFilePathPriorModByRelativePath(folderOrFileRelativePath)];

        // Resources 必须在使用 ParseFileAndAddToResources 之前初始化
        if (isAsyncLoading)
        {
            Resources = new ConcurrentDictionary<string, TContent>();
        }
        else
        {
            Resources = new Dictionary<string, TContent>(filePaths.Length);
        }

        if (isAsyncLoading)
        {
            ParseFileAndAddToResourcesAsync(filePaths);
        }
        else
        {
            SortFilePath(filePaths);
            ParseFileAndAddToResourcesSync(filePaths);
        }

        watcherService.Watch(
            isFolderPath
                ? _folderOrFileRelativePath
                : Path.GetDirectoryName(folderOrFileRelativePath) ?? folderOrFileRelativePath,
            this,
            filter.Name
        );
        Log.Info("初始化资源成功: {FolderRelativePath}, 共 {Count} 个文件", _folderOrFileRelativePath, filePaths.Length);
        LogItemsSum();
    }

    /// <summary>
    /// 当需要对传入的文件路径顺序进行排序时, 重写此方法, 在多线程加载资源时不调用.
    /// </summary>
    /// <param name="filePathArray"></param>
    protected virtual void SortFilePath(string[] filePathArray) { }

    private void ParseFileAndAddToResourcesAsync(string[] filePaths)
    {
        var tasks = new List<Task>();
        foreach (string filePath in filePaths)
        {
            tasks.Add(Task.Run(() => ParseFileAndAddToResources(filePath)));
        }
        Task.WaitAll(tasks);
    }

    private void ParseFileAndAddToResourcesSync(string[] filePaths)
    {
        foreach (string filePath in filePaths)
        {
            ParseFileAndAddToResources(filePath);
        }
    }

    [Conditional("DEBUG")]
    private void LogItemsSum()
    {
        if (typeof(IReadOnlyCollection<object>).IsAssignableFrom(typeof(TContent)))
        {
            Log.Debug(
                "已加载的资源数量: {Count}, Path: '{Path}'",
                _folderOrFileRelativePath,
                Resources.Values.Cast<IReadOnlyCollection<object>>().Sum(content => content.Count)
            );
        }
    }

    void IResourcesService.Add(string folderOrFilePath)
    {
        Log.Debug("添加 Mod 资源: {FolderOrFilePath}", folderOrFilePath);
        Debug.Assert(File.Exists(folderOrFilePath), "必须为文件");

        // 如果新增加的mod资源在原版资源中存在, 移除原版资源, 添加mod资源
        string relativeFilePath = Path.GetRelativePath(_settingService.ModRootFolderPath, folderOrFilePath);
        string gameFilePath = Path.Combine(_settingService.GameRootFolderPath, relativeFilePath);
        bool isRemoved = Resources.Remove(gameFilePath);
        if (isRemoved)
        {
            Log.Info("{ServiceName}: 移除游戏资源成功: {GameFilePath}", _serviceName, gameFilePath);
        }

        ParseFileAndAddToResources(folderOrFilePath);
        OnOnResourceChanged(new ResourceChangedEventArgs(folderOrFilePath));

        Log.Info("{ServiceName}: 添加 Mod 资源成功: {FolderOrFilePath}", _serviceName, folderOrFilePath);
    }

    void IResourcesService.Remove(string folderOrFilePath)
    {
        Log.Debug("{ServiceName}: 移除 Mod 资源: {FolderOrFilePath}", _serviceName, folderOrFilePath);
        if (Directory.Exists(folderOrFilePath))
        {
            foreach (
                string filePath in Directory.GetFileSystemEntries(
                    folderOrFilePath,
                    "*",
                    SearchOption.AllDirectories
                )
            )
            {
                ((IResourcesService)this).Remove(filePath);
            }
        }

        if (Resources.Remove(folderOrFilePath))
        {
            Log.Info("{ServiceName}: 移除 Mod 资源成功", _serviceName);
            string relativeFilePath = Path.GetRelativePath(
                _settingService.ModRootFolderPath,
                folderOrFilePath
            );

            // 如果删除的mod资源在原版资源中存在, 移除mod资源, 添加原版资源
            string gameFilePath = Path.Combine(_settingService.GameRootFolderPath, relativeFilePath);
            if (!File.Exists(gameFilePath))
            {
                return;
            }

            ParseFileAndAddToResources(gameFilePath);
            OnOnResourceChanged(new ResourceChangedEventArgs(folderOrFilePath));

            Log.Info("{ServiceName}: 添加原版游戏资源: {GameFilePath}", _serviceName, gameFilePath);
        }
    }

    void IResourcesService.Reload(string folderOrFilePath)
    {
        Log.Debug("尝试重新加载 Mod 资源: {FolderOrFilePath}", folderOrFilePath);
        if (Directory.Exists(folderOrFilePath))
        {
            Log.Debug("跳过文件夹");
            return;
        }

        bool isRemoved = Resources.Remove(folderOrFilePath);
        bool isAdded = ParseFileAndAddToResources(folderOrFilePath);
        if (!isAdded)
        {
            Log.Info("{ServiceName}: 不加载此 Mod 资源", _serviceName);
            return;
        }

        // 当有移除或有添加时才触发事件
        if (isRemoved || isAdded)
        {
            OnOnResourceChanged(new ResourceChangedEventArgs(folderOrFilePath));
        }
        Log.Info("{ServiceName}: 重新加载 Mod 资源成功", _serviceName);
    }

    void IResourcesService.Renamed(string oldPath, string newPath)
    {
        Log.Debug("Mod 资源重命名: {OldPath} -> {NewPath}", oldPath, newPath);
        if (Directory.Exists(newPath))
        {
            Log.Debug("{ServiceName}: 跳过文件夹", _serviceName);
            return;
        }

        if (Resources.TryGetValue(oldPath, out var countryTags))
        {
            Resources.Add(newPath, countryTags);
        }
        else
        {
            Log.Debug("{ServiceName}: 跳过处理 {NewPath} 重命名", _serviceName, newPath);
            return;
        }
        Resources.Remove(oldPath);

        Log.Info("{ServiceName}: Mod 资源重命名成功", _serviceName);
    }

    /// <summary>
    /// 解析 folderRelativePath 目录下的所有文件, 并将解析结果添加到 <see cref="Resources"/> 中
    /// </summary>
    /// <param name="result">文件解析结果</param>
    /// <returns>文件内资源内容, 当为 <c>null</c> 时表示该服务不对此文件的变化做出响应</returns>
    protected abstract TContent? ParseFileToContent(TParseResult result);

    protected abstract TParseResult? GetParseResult(string filePath);

    /// <summary>
    /// 解析文件, 并将解析结果添加到 <see cref="Resources"/> 中
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>成功添加返回 <c>true</c>, 否则返回 <c>false</c></returns>
    private bool ParseFileAndAddToResources(string filePath)
    {
        var result = GetParseResult(filePath);
        return AddToResources(filePath, result);
    }

    private bool AddToResources(string filePath, TParseResult? result)
    {
        if (result is null)
        {
            Log.Warn("{ServiceName}: 文件 {FilePath} 解析失败", _serviceName, filePath);
            return false;
        }

        var content = ParseFileToContent(result);
        if (content is null)
        {
            return false;
        }

        Resources.Add(filePath, content);
        return true;
    }

    private void OnOnResourceChanged(ResourceChangedEventArgs e)
    {
        OnResourceChanged?.Invoke(this, e);
    }
}
