using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using ParadoxPower.Process;
using VModer.Core.Analyzers;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;

namespace VModer.Core.Services;

public sealed class AnalyzeService
{
    private readonly GameFilesService _gameFilesService;
    private readonly EditorDiagnosisService _editorDiagnosisService;
    private readonly StateAnalyzerService _stateAnalyzerService;
    private readonly CharacterAnalyzerService _characterAnalyzerService;
    private readonly SettingsService _settingsService;

    public AnalyzeService(
        GameFilesService gameFilesService,
        EditorDiagnosisService editorDiagnosisService,
        StateAnalyzerService stateAnalyzerService,
        CharacterAnalyzerService characterAnalyzerService,
        SettingsService settingsService
    )
    {
        _gameFilesService = gameFilesService;
        _editorDiagnosisService = editorDiagnosisService;
        _stateAnalyzerService = stateAnalyzerService;
        _characterAnalyzerService = characterAnalyzerService;
        _settingsService = settingsService;
    }

    public Task AnalyzeAllFilesAsync()
    {
        foreach (
            var fileUri in Directory.EnumerateFiles(
                _settingsService.ModRootFolderPath,
                "*.txt",
                SearchOption.AllDirectories
            )
        )
        {
            // _ = AnalyzeFileAsync(fileUri);
        }

        return Task.CompletedTask;
    }

    public Task AnalyzeFileAsync(Uri fileUri)
    {
        if (!_gameFilesService.TryGetFileText(fileUri, out string? fileText))
        {
            return Task.CompletedTask;
        }

        string filePath = fileUri.ToSystemPath();

        if (!TextParser.TryParse(filePath, fileText, out var node, out var error))
        {
            return _editorDiagnosisService.AddDiagnoseAsync(error, fileUri);
        }

        var gameFileType = GameFileType.FromFilePath(filePath);

        var diagnoses = AnalyzeFile(node, filePath, gameFileType);
        return _editorDiagnosisService.AddDiagnoseAsync(
            new PublishDiagnosticsParams { Diagnostics = diagnoses, Uri = fileUri }
        );
    }

    private List<Diagnostic> AnalyzeFile(Node node, string filePath, GameFileType gameFileType)
    {
        return gameFileType.Name switch
        {
            nameof(GameFileType.State) => _stateAnalyzerService.Analyze(node, filePath),
            nameof(GameFileType.Character) => _characterAnalyzerService.Analyze(node),
            _ => EmptyDiagnoses
        };
    }

    private static readonly List<Diagnostic> EmptyDiagnoses = [];
}
