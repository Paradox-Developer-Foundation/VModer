using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using MethodTimer;
using ParadoxPower.CSharpExtensions;
using VModer.Core.Extensions;
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
    public HoverResponse? GetHoverResponse(HoverParams request)
    {
        string filePath = request.TextDocument.Uri.Uri.ToSystemPath();
        if (!_gameFilesService.TryGetFileText(request.TextDocument.Uri.Uri, out string? text))
        {
            return null;
        }
        if (!TextParser.TryParse(filePath, text, out var rootNode, out _))
        {
            return null;
        }

        var fileType = GameFileType.FromFilePath(filePath);
        string hoverText = _hoverStrategyManager.GetHoverText(fileType, rootNode, request);
        if (string.IsNullOrEmpty(hoverText))
        {
            return null;
        }

        return new HoverResponse
        {
            Contents = new MarkupContent { Kind = MarkupKind.Markdown, Value = hoverText }
        };
    }
}
