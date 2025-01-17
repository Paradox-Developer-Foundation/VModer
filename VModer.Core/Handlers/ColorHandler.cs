using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentColor;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using VModer.Core.Services;

namespace VModer.Core.Handlers;

public sealed class DocumentColorHandler : DocumentColorHandlerBase, IHandler
{
    private DocumentColorService _documentColorService = null!;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    protected override Task<DocumentColorResponse> Handle(
        DocumentColorParams request,
        CancellationToken token
    )
    {
        return _documentColorService.GetDocumentColorAsync(request);
    }

    protected override Task<ColorPresentationResponse> Resolve(
        ColorPresentationParams request,
        CancellationToken token
    )
    {
        return Task.FromResult(_documentColorService.GetColorPresentation(request));
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities
    )
    {
        serverCapabilities.ColorProvider = true;
    }

    public void Initialize()
    {
        _documentColorService = App.Services.GetRequiredService<DocumentColorService>();
    }
}
