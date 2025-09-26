using System.Text.Json;
using EmmyLua.LanguageServer.Framework.Protocol.Message.CodeAction;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using ParadoxPower.CSharpExtensions;
using VModer.Core.Analyzers;
using VModer.Core.Models;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Services;

public sealed class CodeActionService(GameFilesService gameFilesService)
{
    public CodeActionResponse GetCodeActions(CodeActionParams request)
    {
        var list = new List<CodeAction>(2);

        foreach (var diagnostic in request.Context.Diagnostics)
        {
            if (diagnostic.Code?.StringValue == ErrorCode.VM2000)
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
        }

        return new CodeActionResponse(list);
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
