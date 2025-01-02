using System.Diagnostics.CodeAnalysis;
using System.Text;
using EmmyLua.LanguageServer.Framework.Protocol.Message.TextDocument;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using NLog;

namespace VModer.Core.Services;

public sealed class GameFilesService
{
    private readonly Dictionary<Uri, StringBuilder> _openedFiles = new(8);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void OnFileChanged(DidChangeTextDocumentParams request)
    {
        foreach (var contentChangeEvent in request.ContentChanges)
        {
            if (contentChangeEvent.Range.HasValue)
            {
                PatchText(
                    request.TextDocument.Uri.Uri,
                    contentChangeEvent.Range.Value,
                    contentChangeEvent.Text
                );
            }
            else
            {
                ReplaceText(request.TextDocument.Uri.Uri, contentChangeEvent.Text);
            }
        }
    }

    private void PatchText(Uri uri, DocumentRange range, string text)
    {
        if (!_openedFiles.TryGetValue(uri, out var existing))
        {
            return;
        }

        (int startOffset, int endOffset) = FindRange(existing, range);
        existing.Remove(startOffset, endOffset - startOffset);
        existing.Insert(startOffset, text);
    }

    private void ReplaceText(Uri uri, string fileText)
    {
        Log.Info("ReplaceFile: {}", uri);
        if (!_openedFiles.TryGetValue(uri, out var existing))
        {
            return;
        }

        existing.Clear();
        existing.Append(fileText);
    }

    private static (int startOffset, int endOffset) FindRange(StringBuilder text, DocumentRange range)
    {
        int line = 0;
        int charPos = 0;
        int startOffset = 0;
        int endOffset = 0;

        for (int offset = 0; offset <= text.Length; offset++)
        {
            if (line == range.Start.Line && charPos == range.Start.Character)
            {
                startOffset = offset;
            }

            if (line == range.End.Line && charPos == range.End.Character)
            {
                endOffset = offset;
            }

            if (offset < text.Length)
            {
                char c = text[offset];
                if (c == '\n')
                {
                    line++;
                    charPos = 0;
                }
                else
                {
                    charPos++;
                }
            }
        }

        return (startOffset, endOffset);
    }

    public void AddFile(Uri filePathUri, string fileText)
    {
        _openedFiles[filePathUri] = new StringBuilder(fileText);
    }

    public void RemoveFile(Uri filePathUri)
    {
        _openedFiles.Remove(filePathUri);
    }

    public bool TryGetFileText(Uri filePathUri, [NotNullWhen(true)] out string? fileText)
    {
        if (!_openedFiles.TryGetValue(filePathUri, out var fileTextBuilder))
        {
            fileText = null;
            return false;
        }

        // TODO: 能否不额外创建字符串?
        fileText = fileTextBuilder.ToString();
        return true;
    }
}
