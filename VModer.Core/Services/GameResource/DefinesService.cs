using Neo.IronLua;
using VModer.Core.Infrastructure;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class DefinesService
    : ResourcesService<
        DefinesService,
        (string FilePath, Lua Lua, LuaGlobal LuaGlobal),
        (string FilePath, Lua Lua, LuaGlobal LuaGlobal)
    >,
        IDisposable
{
    private LuaGlobal[] SortedLua => _sortedLuaLazy.Value;
    private readonly ResetLazy<LuaGlobal[]> _sortedLuaLazy;

    public DefinesService(GameResourcesPathService pathService)
        : base(Path.Combine(Keywords.Common, "defines"), WatcherFilter.Lua, PathType.Folder)
    {
        _sortedLuaLazy = new ResetLazy<LuaGlobal[]>(
            () =>
                Resources
                    .Values.OrderByDescending(tuple =>
                        pathService.GetFilePathType(tuple.FilePath) == GameResourcesPathService.FileType.Mod
                    )
                    .Select(tuple => tuple.LuaGlobal)
                    .ToArray()
        );

        OnResourceChanged += (_, _) => _sortedLuaLazy.Reset();
    }

    public T? Get<T>(string defineName)
    {
        foreach (var lua in SortedLua)
        {
            // ReSharper disable once CoVariantArrayConversion
            object? value = lua.GetValue(defineName.Split('.'));
            if (value is not null)
            {
                return (T)value;
            }
        }

        return default;
    }

    protected override (string FilePath, Lua Lua, LuaGlobal LuaGlobal) ParseFileToContent(
        (string FilePath, Lua Lua, LuaGlobal LuaGlobal) result
    )
    {
        return result;
    }

    protected override (string, Lua, LuaGlobal) GetParseResult(string filePath)
    {
        var lua = new Lua();
        var env = lua.CreateEnvironment();
        env.DoChunk("NDefines = {}", "patch.lua");
        env.DoChunk(filePath);

        return (filePath, lua, env);
    }

    public void Dispose()
    {
        foreach (var value in Resources.Values)
        {
            value.Lua.Dispose();
        }
    }
}
