using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.CodeAction;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using VModer.Core.Services;

namespace VModer.Core.Handlers;

public sealed class CodeActionHandler : CodeActionHandlerBase, IHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private CodeActionService _codeActionService = null!;

    protected override Task<CodeActionResponse> Handle(CodeActionParams request, CancellationToken token)
    {
        Log.Info("CodeActionParams {@A}", request);
        return Task.Run(() => _codeActionService.GetCodeActions(request), token);
    }

    protected override Task<CodeAction> Resolve(CodeAction request, CancellationToken token)
    {
        Log.Info("CodeAction {@A}", request);
        return Task.FromResult(request);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities
    )
    {
        serverCapabilities.CodeActionProvider = new CodeActionOptions
        {
            ResolveProvider = true,
            CodeActionKinds = [CodeActionKind.QuickFix, CodeActionKind.SourceFixAll]
        };
    }

    public void Initialize()
    {
        _codeActionService = App.Services.GetRequiredService<CodeActionService>();
    }
}
