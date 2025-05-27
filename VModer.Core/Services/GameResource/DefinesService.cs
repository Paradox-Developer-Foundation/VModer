using Microsoft.Extensions.DependencyInjection;
using Neo.IronLua;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

// 使用 byte 是因为不需要分开存储各个文件中的变量, 而是集中在一个静态只读字段中, 使用 byte 只是为了最小化浪费内存
// BUG: 当部分定义在 Mod 中被修改后, 如果之后删除了 Mod, 那么定义将无法被恢复到游戏定义值
// 解决方案: 重新读取游戏文件?
public sealed class DefinesService : ResourcesService<DefinesService, byte, byte>, IDisposable
{
    private static readonly Lua GlobalLua = new();
    private static readonly LuaGlobal GlobalEnv = GlobalLua.CreateEnvironment();

    public DefinesService()
        : base(Path.Combine(Keywords.Common, "defines"), WatcherFilter.Lua, PathType.Folder) { }

    protected override void SortFilePath(string[] filePathArray)
    {
        var pathService = App.Services.GetRequiredService<GameResourcesPathService>();

        Array.Sort(
            filePathArray,
            (x, y) =>
            {
                int xPriority = GetFilePathPriority(x, pathService);
                int yPriority = GetFilePathPriority(y, pathService);

                return xPriority.CompareTo(yPriority);
            }
        );
    }

    private static int GetFilePathPriority(string filePath, GameResourcesPathService pathService)
    {
        if (Path.GetFileName(filePath).Equals("00_defines.lua", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var fileType = pathService.GetFileOrigin(filePath);
        if (fileType == FileOrigin.Game)
        {
            return 1;
        }

        return 2;
    }

    public T? Get<T>(string[] defineName)
    {
        // TODO: 缓存一下

        // ReSharper disable once CoVariantArrayConversion
        object? value = GlobalEnv.GetValue(defineName);
        if (value is not null)
        {
            return (T)value;
        }

        return default;
    }

    protected override byte ParseFileToContent(byte result)
    {
        return result;
    }

    protected override byte GetParseResult(string filePath)
    {
        GlobalEnv.DoChunk(filePath);

        return 0;
    }

    public void Dispose()
    {
        GlobalLua.Dispose();
    }
}
