using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Completion;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using VModer.Core.Services;

namespace VModer.Core.Handlers;

public sealed class CompletionHandler : CompletionHandlerBase, IHandler
{
    private CompletionService _completionService = null!;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    protected override Task<CompletionResponse?> Handle(CompletionParams request, CancellationToken token)
    {
        // Log.Info("Completion request: {@Request}", request);
        return Task.FromResult<CompletionResponse?>(_completionService.Resolve(request));
    }

    protected override Task<CompletionItem> Resolve(CompletionItem item, CancellationToken token)
    {
        return Task.FromResult(item);
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities
    )
    {
        serverCapabilities.CompletionProvider = new CompletionOptions
        {
            ResolveProvider = true,
            TriggerCharacters = ["."]
        };
    }

    public void Initialize()
    {
        _completionService = App.Services.GetRequiredService<CompletionService>();
    }
}
