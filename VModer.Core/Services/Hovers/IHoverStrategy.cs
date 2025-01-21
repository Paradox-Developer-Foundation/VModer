using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using ParadoxPower.Process;
using VModer.Core.Models;

namespace VModer.Core.Services.Hovers;

public interface IHoverStrategy
{
    /// <summary>
    /// 这个策略负责处理的文件类型
    /// </summary>
    GameFileType FileType { get; }
    public string GetHoverText(Node rootNode, HoverParams request);
}