using EmmyLua.LanguageServer.Framework.Protocol.Message.Completion;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Kind;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Union;
using MethodTimer;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;
using VModer.Core.Services.GameResource;

namespace VModer.Core.Services;

public sealed class CompletionService
{
    private readonly GameFilesService _filesService = App.Services.GetRequiredService<GameFilesService>();
    private readonly OreService _oreService = App.Services.GetRequiredService<OreService>();

    private static readonly CompletionResponse EmptyResponse = new([]);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [Time]
    public CompletionResponse Resolve(CompletionParams request)
    {
        if (!_filesService.TryGetFileText(request.TextDocument.Uri.Uri, out var fileText))
        {
            return EmptyResponse;
        }

        var filePath = request.TextDocument.Uri.Uri.ToSystemPath();
        var type = GameFileType.FromFilePath(filePath);
        // TODO: 跟分析服务中的解析能否合并?
        if (!TextParser.TryParse(filePath, fileText, out var rootNode, out _))
        {
            return EmptyResponse;
        }

        var node = FindNodeByPosition(rootNode, request.Position.ToLocalPosition());
        Log.Debug("Key: {P}, file type: {}", node.Key, type);

        return GetCompletion(node, type);
    }

    /// <summary>
    /// 获取离光标最近的 <see cref="Node"/>
    /// </summary>
    /// <param name="node">节点</param>
    /// <param name="cursorPosition">光标位置</param>
    /// <returns>离光标最近的 <see cref="Node"/></returns>
    private static Node FindNodeByPosition(Node node, Position cursorPosition)
    {
        foreach (var child in node.Nodes)
        {
            var childPosition = child.Position;
            Log.Trace(
                "Child: {P}, cursor:{CursorPosition}, Key: {}",
                child.Position,
                cursorPosition,
                child.Key
            );
            if (
                (
                    cursorPosition.Line == childPosition.StartLine
                    && cursorPosition.Character > childPosition.StartColumn
                )
                || (
                    cursorPosition.Line == childPosition.EndLine
                    && cursorPosition.Character < childPosition.EndColumn
                )
            )
            {
                return child;
            }

            if (cursorPosition.Line > childPosition.StartLine && cursorPosition.Line < childPosition.EndLine)
            {
                return FindNodeByPosition(child, cursorPosition);
            }
        }

        return node;
    }

    private CompletionResponse GetCompletion(Node node, GameFileType fileType)
    {
        return fileType.Name switch
        {
            nameof(GameFileType.State) => GetCompletionForState(node),
            _ => EmptyResponse
        };
    }

    private CompletionResponse GetCompletionForState(Node node)
    {
        var list = new List<CompletionItem>();

        if (node.Key.Equals("resources", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var ore in _oreService.AllOres)
            {
                list.Add(
                    new CompletionItem
                    {
                        Label = ore,
                        Kind = CompletionItemKind.Keyword,
                        Documentation = _oreService.GetLocalizationName(ore),
                        InsertText = $"{ore} = "
                    }
                );
            }
        }

        return new CompletionResponse(list);
    }
}
