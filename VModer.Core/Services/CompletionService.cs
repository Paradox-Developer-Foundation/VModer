using EmmyLua.LanguageServer.Framework.Protocol.Message.Completion;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.Process;
using VModer.Core.Models;
using VModer.Core.Services.GameResource;

namespace VModer.Core.Services;

public sealed class CompletionService
{
    private readonly GameFilesService _filesService = App.Services.GetRequiredService<GameFilesService>();
    private readonly OreService _oreService = App.Services.GetRequiredService<OreService>();
    private readonly CountryTagService _countryTagService =
        App.Services.GetRequiredService<CountryTagService>();

    private static readonly CompletionResponse EmptyResponse = new([]);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // [Time]
    // public CompletionResponse Resolve(CompletionParams request)
    // {
    //     if (!_filesService.TryGetFileText(request.TextDocument.Uri.Uri, out string? fileText))
    //     {
    //         return EmptyResponse;
    //     }
    //
    //     string filePath = request.TextDocument.Uri.Uri.ToSystemPath();
    //     var type = GameFileType.FromFilePath(filePath);
    //     // TODO: 跟分析服务中的解析能否合并?
    //     if (!TextParser.TryParse(filePath, fileText, out var rootNode, out _))
    //     {
    //         // 当输入未完成时, 无法解析为 AST, 则尝试获取光标前的字符串
    //         return GetCompletionByStringBeforeCursor(fileText, request.Position);
    //     }
    //
    //     var node = FindNodeByPosition(rootNode, request.Position.ToLocalPosition());
    //     Log.Debug("Key: {P}, file type: {}", node.Key, type);
    //
    //     return GetCompletion(node, type);
    // }

    private CompletionResponse GetCompletionByStringBeforeCursor(string fileText, Position cursorPosition)
    {
        var list = new List<CompletionItem>();
        // 计算光标前的字符串不需要转换位置
        var leftString = GetStringBeforeCursor(fileText, cursorPosition);
        Log.Debug("Left string: {P}", leftString.ToString());
        if (leftString.Equals("add_core_of".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            list.EnsureCapacity(_countryTagService.CountryTags.Count);
            foreach (string countryTag in _countryTagService.CountryTags)
            {
                list.Add(new CompletionItem { Label = countryTag, Kind = CompletionItemKind.Keyword });
            }
        }

        return new CompletionResponse(list);
    }

    /// <summary>
    /// 获取光标前的字符串
    /// </summary>
    /// <param name="fileText"></param>
    /// <param name="cursorPosition"></param>
    /// <returns>光标前的字符串</returns>
    private static ReadOnlySpan<char> GetStringBeforeCursor(string fileText, Position cursorPosition)
    {
        int line = 0;
        ReadOnlySpan<char> cursorCurrentLineString = null;
        foreach (var enumerateLine in fileText.AsSpan().EnumerateLines())
        {
            if (line == cursorPosition.Line)
            {
                cursorCurrentLineString = enumerateLine;
                break;
            }
            line++;
        }

        if (cursorCurrentLineString.IsEmpty)
        {
            return cursorCurrentLineString;
        }

        int startIndex = GetStartIndex(cursorCurrentLineString);
        int length = 0;
        for (int i = startIndex; i < cursorCurrentLineString.Length && i < cursorPosition.Character; i++)
        {
            char c = cursorCurrentLineString[i];
            if (c != ' ' && c != '=' && c != '{' && c != '}' && c != '\t')
            {
                length++;
            }
            else
            {
                break;
            }
        }

        return cursorCurrentLineString.Slice(startIndex, length);
    }

    /// <summary>
    /// 跳过多余字符, 获取真正地开始位置
    /// </summary>
    /// <param name="lineSpan"></param>
    /// <returns></returns>
    private static int GetStartIndex(ReadOnlySpan<char> lineSpan)
    {
        int startIndex = 0;
        while (startIndex < lineSpan.Length)
        {
            char currentChar = lineSpan[startIndex];
            if (currentChar is '{' or '}' or ' ' or '=' or '\t')
            {
                ++startIndex;
            }
            else
            {
                break;
            }
        }

        return startIndex;
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
            foreach (string ore in _oreService.AllOres)
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
