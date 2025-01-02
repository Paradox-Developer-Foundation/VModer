using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using EmmyLua.LanguageServer.Framework.Server;
using NLog;
using NLog.Fluent;
using ParadoxPower.CSharp;

namespace VModer.Core.Services;

public sealed class EditorDiagnosisService
{
    private readonly LanguageServer _server;

    private static readonly List<Diagnostic> EmptyDiagnostics = [];
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorDiagnosisService(LanguageServer server)
    {
        _server = server;
    }

    public Task AddDiagnoseAsync(ParserError error, Uri filePath)
    {
        var range = GetRange(error, 0);
        // Log.Debug("position: {@}", range);
        return AddDiagnoseAsync(
            new PublishDiagnosticsParams
            {
                // TODO: Markup?
                Diagnostics =
                [
                    new Diagnostic
                    {
                        Code = "VM1000",
                        Range = range,
                        Message = error.ErrorMessage,
                        Severity = DiagnosticSeverity.Error
                    }
                ],
                Uri = filePath
            }
        );
    }

    private static DocumentRange GetRange(ParserError position, int length)
    {
        int startChar;
        int endChar;
        var startLine = Math.Max((int)position.Line - 1, 0);
        if (length == 0)
        {
            startChar = 0;
            endChar = (int)position.Column;
        }
        else
        {
            startChar = (int)position.Column;
            endChar = (int)position.Column + length;
        }
        var start = new Position(startLine, startChar);
        var end = new Position(startLine, endChar);
        return DocumentRange.From(start, end);
    }

    public Task AddDiagnoseAsync(PublishDiagnosticsParams diagnoseParams)
    {
        return _server.Client.PublishDiagnostics(diagnoseParams);
    }

    public Task ClearDiagnoseAsync(Uri fileUri)
    {
        return _server.Client.PublishDiagnostics(
            new PublishDiagnosticsParams { Diagnostics = EmptyDiagnostics, Uri = fileUri }
        );
    }
}
