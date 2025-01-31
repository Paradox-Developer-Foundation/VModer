using System.Text;
using NLua;
using VModer.Core.Infrastructure;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class DefinesService
    : ResourcesService<DefinesService, (string FilePath, Lua Lua), (string FilePath, Lua Lua)>,
        IDisposable
{
    private Lua[] SortedLua => _sortedLuaLazy.Value;
    private readonly ResetLazy<Lua[]> _sortedLuaLazy;

    public DefinesService(GameResourcesPathService pathService)
        : base(Path.Combine(Keywords.Common, "defines"), WatcherFilter.Lua, PathType.Folder)
    {
        _sortedLuaLazy = new ResetLazy<Lua[]>(
            () =>
                Resources
                    .Values.OrderByDescending(tuple =>
                        pathService.GetFilePathType(tuple.FilePath) == GameResourcesPathService.FileType.Mod
                    )
                    .Select(tuple => tuple.Lua)
                    .ToArray()
        );

        OnResourceChanged += (_, _) => _sortedLuaLazy.Reset();
    }

    public T? Get<T>(string defineName)
    {
        foreach (var lua in SortedLua)
        {
            object? value = lua[defineName];
            if (value is not null)
            {
                return (T)value;
            }
        }

        return default;
    }

    protected override (string FilePath, Lua Lua) ParseFileToContent((string FilePath, Lua Lua) result)
    {
        return result;
    }

    protected override (string, Lua) GetParseResult(string filePath)
    {
        var lua = new Lua();
        lua.State.Encoding = Encoding.UTF8;
        lua.DoString("NDefines = {}");
        lua.DoFile(filePath);

        return (filePath, lua);
    }

    public void Dispose()
    {
        foreach (var value in Resources.Values)
        {
            value.Lua.Dispose();
        }
    }
}
