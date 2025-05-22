using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.CodeAction;
using EmmyLua.LanguageServer.Framework.Server.Handler;

namespace VModer.Core.Handlers;

public sealed class CodeActionHandler : CodeActionHandlerBase
{
    protected override Task<CodeActionResponse> Handle(CodeActionParams request, CancellationToken token)
    {
        return Task.FromResult(
            new CodeActionResponse(
                new List<CodeAction>
                {
                    new() { Title = "123", Diagnostics = request.Context.Diagnostics }
                }
            )
        );
    }

    protected override Task<CodeAction> Resolve(CodeAction request, CancellationToken token)
    {
        return Task.FromResult(request);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities
    )
    {
        serverCapabilities.CodeActionProvider = new CodeActionOptions
        {
            CodeActionKinds = [CodeActionKind.QuickFix]
        };
    }
}
