using EmmyLua.LanguageServer.Framework.Server.Handler;

namespace VModer.Core.Handlers;

public interface IHandler : IJsonHandler
{
    /// <summary>
    /// 初始化需要依赖注入的资源
    /// </summary>
    /// <remarks>
    /// 因为大部分资源都需要 VS Code 传来的参数信息(比如 游戏根目录路径信息)，而所以需要在初始化后
    /// </remarks>
    void Initialize();
}