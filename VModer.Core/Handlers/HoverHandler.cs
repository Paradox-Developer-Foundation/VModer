using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Microsoft.Extensions.DependencyInjection;
using VModer.Core.Services.Hovers;

namespace VModer.Core.Handlers;

public sealed class HoverHandler : HoverHandlerBase, IHandler
{
    
    private HoverService _hoverService = null!;

    protected override Task<HoverResponse?> Handle(HoverParams request, CancellationToken token)
    {
        return _hoverService.GetHoverResponseAsync(request);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities
    )
    {
        serverCapabilities.HoverProvider = true;
    }

    public void Initialize()
    {
        _hoverService = App.Services.GetRequiredService<HoverService>();
    }
}
