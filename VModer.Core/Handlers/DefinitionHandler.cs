using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Definition;
using EmmyLua.LanguageServer.Framework.Server.Handler;

namespace VModer.Core.Handlers;

public sealed class DefinitionHandler : DefinitionHandlerBase, IHandler
{
    protected override Task<DefinitionResponse?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        // request.Position;
        // var tes = new DefinitionResponse(new Location());
        return Task.FromResult<DefinitionResponse?>(null);
    }

    public override void RegisterCapability(ServerCapabilities serverCapabilities, ClientCapabilities clientCapabilities)
    {
        serverCapabilities.DefinitionProvider = true;
    }

    public void Initialize()
    {
        
    }
}