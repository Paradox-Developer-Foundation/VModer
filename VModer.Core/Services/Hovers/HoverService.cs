using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using MethodTimer;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;

namespace VModer.Core.Services.Hovers;

public sealed class HoverService
{
    private readonly GameFilesService _gameFilesService;
    private readonly HoverStrategyManager _hoverStrategyManager;

    public HoverService(GameFilesService gameFilesService, HoverStrategyManager hoverStrategyManager)
    {
        _gameFilesService = gameFilesService;
        _hoverStrategyManager = hoverStrategyManager;
    }

    [Time]
    public Task<HoverResponse?> GetHoverResponseAsync(HoverParams request)
    {
        string filePath = request.TextDocument.Uri.Uri.ToSystemPath();
        if (!_gameFilesService.TryGetFileText(request.TextDocument.Uri.Uri, out string? text))
        {
            return Task.FromResult<HoverResponse?>(null);
        }
        if (!TextParser.TryParse(filePath, text, out var rootNode, out _))
        {
            return Task.FromResult<HoverResponse?>(null);
        }

        var fileType = GameFileType.FromFilePath(filePath);
        string hoverText = _hoverStrategyManager.GetHoverText(fileType, rootNode, request);
        return Task.FromResult<HoverResponse?>(
            new HoverResponse
            {
                Contents = new MarkupContent { Kind = MarkupKind.Markdown, Value = hoverText }
            }
        );
    }
}
