using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using EmmyLua.LanguageServer.Framework.Server.Handler;

namespace VModer.Core.Handlers;

public class HoverHandler : HoverHandlerBase, IHandler
{
    protected override Task<HoverResponse?> Handle(HoverParams request, CancellationToken token)
    {
        return Task.FromResult<HoverResponse?>(new HoverResponse
        {
            Contents = new MarkupContent
            {
                Kind = MarkupKind.PlainText,
                Value = "Hello World!"
            }
        });
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities, ClientCapabilities clientCapabilities)
    {
        serverCapabilities.HoverProvider = true;
    }

    public void Initialize()
    {
        
    }
}