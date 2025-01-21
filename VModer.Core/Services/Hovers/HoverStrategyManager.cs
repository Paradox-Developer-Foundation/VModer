using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Microsoft.Extensions.DependencyInjection;
using ParadoxPower.Process;
using VModer.Core.Models;

namespace VModer.Core.Services.Hovers;

public sealed class HoverStrategyManager
{
    private readonly IHoverStrategy[] _strategies;
    private readonly IHoverStrategy _defaultStrategy;

    public HoverStrategyManager()
    {
        _strategies = App.Services.GetServices<IHoverStrategy>().ToArray();
        _defaultStrategy = _strategies.First(strategy => strategy.FileType == GameFileType.Unknown);
    }

    public string GetHoverText(GameFileType fileType, Node rootNode, HoverParams request)
    {
        foreach (var strategy in _strategies)
        {
            if (fileType == strategy.FileType)
            {
                return strategy.GetHoverText(rootNode, request);
            }
        }

        return _defaultStrategy.GetHoverText(rootNode, request);
    }
}
