using EmmyLua.LanguageServer.Framework.Protocol.Message.CodeAction;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using VModer.Languages;

namespace VModer.Core.Services;

public sealed class CodeActionService
{
    public CodeActionResponse GetCodeActions(CodeActionParams request)
    {
        var list = new List<CodeAction>(1);

        foreach (var diagnostic in request.Context.Diagnostics)
        {
            if (diagnostic.Code?.StringValue == ErrorCode.VM1005)
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
                            {
                                request.TextDocument.Uri,
                                [new TextEdit { Range = diagnostic.Range, NewText = string.Empty }]
                            }
                        }
                    }
                };
                list.Add(codeAction);
            }
        }
        return new CodeActionResponse(list);
    }
}
