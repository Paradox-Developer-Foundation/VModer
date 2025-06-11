using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using EmmyLua.LanguageServer.Framework.Server;
using ParadoxPower.CSharp;

namespace VModer.Core.Services;

public sealed class EditorDiagnosisService
{
    private readonly LanguageServer _server;

    public EditorDiagnosisService(LanguageServer server)
    {
        _server = server;
    }

    public Task AddDiagnoseAsync(PublishDiagnosticsParams diagnoseParams)
    {
        return _server.Client.PublishDiagnostics(diagnoseParams);
    }
}
