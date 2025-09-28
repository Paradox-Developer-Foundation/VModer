using System.Text.Json;
using EmmyLua.LanguageServer.Framework.Protocol.Message.CodeAction;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using ParadoxPower.CSharpExtensions;
using VModer.Core.Analyzers;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Services.GameResource;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Services;

public sealed class CodeActionService(
    GameFilesService gameFilesService,
    StatesProvincesMapService provincesService
)
{
    public CodeActionResponse GetCodeActions(CodeActionParams request)
    {
        var list = new List<CodeAction>(2);

        foreach (var diagnostic in request.Context.Diagnostics)
        {
            string? errorCode = diagnostic.Code?.StringValue;
            if (errorCode == ErrorCode.VM2000)
            {
                var codeAction = new CodeAction
                {
                    Title = Resources.QuickFix_RemoveUnusedStatement,
                    Kind = CodeActionKind.QuickFix,
                    IsPreferred = true,
                    Diagnostics = [diagnostic],
                    Edit = new WorkspaceEdit
                    {
                        Changes = new Dictionary<DocumentUri, List<TextEdit>>
                        {
                            { request.TextDocument.Uri, [GetTextEditForRemoveUnusedStatement(diagnostic)] }
                        }
                    }
                };
                list.Add(codeAction);
                list.Add(
                    new CodeAction
                    {
                        Title = $"{Resources.FixAll}{Resources.QuickFix_RemoveUnusedStatement}",
                        Kind = CodeActionKind.QuickFix,
                        Data = new LSPAny(
                            JsonSerializer.Serialize(
                                new CodeActionData(ErrorCode.VM2000, request.TextDocument.Uri.Uri),
                                CodeActionDataContext.Default.CodeActionData
                            )
                        )
                    }
                );
            }
            else if (errorCode == ErrorCode.VM2001)
            {
                var codeAction = GetQuickFixForMoveVictoryPointsToOccupiedStateFile(diagnostic, request);
                if (codeAction is null)
                {
                    continue;
                }
                list.Add(codeAction);
            }
        }

        return new CodeActionResponse(list);
    }

    private CodeAction? GetQuickFixForMoveVictoryPointsToOccupiedStateFile(
        Diagnostic diagnostic,
        CodeActionParams request
    )
    {
        if (diagnostic.Data?.Value is not string json)
        {
            return null;
        }

        var victoryPoint = JsonSerializer.Deserialize(json, VictoryPointContext.Default.VictoryPoint);

        var stateFilePath = provincesService.FindStatesFileByProvince(victoryPoint.ProvinceId);
        if (string.IsNullOrEmpty(stateFilePath))
        {
            return null;
        }

        var stateFilePathUri = new Uri(stateFilePath, UriKind.Absolute);
        if (
            !gameFilesService.TryGetParsedFile(stateFilePathUri, out var rootNode)
            && !TextParser.TryParse(stateFilePath, out rootNode, out _)
        )
        {
            return null;
        }

        if (!rootNode.TryGetNode(out var historyNode, Keywords.State, Keywords.History))
        {
            return null;
        }

        string suffix = string.Empty;
        var position = historyNode.Position.ToDocumentRange();
        Position insertPosition;
        if (position.Start.Line == position.End.Line)
        {
            insertPosition = new Position(
                position.Start.Line,
                position.End.Character - 1
            );
            suffix = "\n\t";
        }
        else
        {
            insertPosition = new Position(
                position.End.Line,
                int.MaxValue
            );
        }
        position = new DocumentRange(insertPosition, insertPosition);

        var codeAction = new CodeAction
        {
            Title = Resources.QuickFix_MoveVictoryPointsToOccupiedStateFile,
            Kind = CodeActionKind.QuickFix,
            IsPreferred = true,
            Edit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, List<TextEdit>>(2)
                {
                    {
                        request.TextDocument.Uri,
                        [new TextEdit { Range = diagnostic.Range, NewText = string.Empty }]
                    },
                    {
                        stateFilePathUri,
                        [
                            new TextEdit
                            {
                                Range = position,
                                NewText =
                                    $"\n\t\tvictory_points = {{ {victoryPoint.ProvinceId} {victoryPoint.Value} }}{suffix}"
                            }
                        ]
                    }
                }
            }
        };

        return codeAction;
    }

    public CodeAction GetCodeAction(CodeAction request)
    {
        var data =
            JsonSerializer.Deserialize<CodeActionData>(
                (string?)request.Data?.Value ?? throw new ArgumentException(),
                CodeActionDataContext.Default.CodeActionData
            ) ?? throw new ArgumentException();

        if (data.ErrorCode == ErrorCode.VM2000)
        {
            return GetCodeActionForRemoveUnusedStatement(request, data.FilePath);
        }

        return request;
    }

    private CodeAction GetCodeActionForRemoveUnusedStatement(CodeAction request, Uri filePath)
    {
        if (
            !gameFilesService.TryGetFileText(filePath, out string? text)
            || !TextParser.TryParse(string.Empty, text, out var rootNode, out _)
        )
        {
            return request;
        }

        var list = new List<Diagnostic>(16);
        AnalyzeHelper.AnalyzeEmptyNode(rootNode, list);
        if (list.Count == 0)
        {
            return request;
        }

        var edits = list.AsValueEnumerable().Select(GetTextEditForRemoveUnusedStatement).ToList();
        request.Edit ??= new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, List<TextEdit>>(1) { { filePath, edits } }
        };
        return request;
    }

    private static TextEdit GetTextEditForRemoveUnusedStatement(Diagnostic diagnostic)
    {
        return new TextEdit { Range = diagnostic.Range, NewText = string.Empty };
    }
}
