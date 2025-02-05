using System.Diagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using NLog;
using ParadoxPower.Process;
using VModer.Core.Analyzers;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;

namespace VModer.Core.Services;

public sealed class AnalyzeService(
    GameFilesService gameFilesService,
    EditorDiagnosisService editorDiagnosisService,
    StateAnalyzerService stateAnalyzerService,
    CharacterAnalyzerService characterAnalyzerService,
    SettingsService settingsService
)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task AnalyzeAllFilesAsync(CancellationToken cancellationToken)
    {
        long start = Stopwatch.GetTimestamp();
        var tasks = new List<Task>();
        foreach (
            string filePath in Directory.EnumerateFiles(
                settingsService.ModRootFolderPath,
                "*.txt",
                SearchOption.AllDirectories
            )
        )
        {
            tasks.Add(
                Task.Run(
                    async () => await AnalyzeFileFromFilePathAsync(filePath).ConfigureAwait(false),
                    cancellationToken
                )
            );
        }

        return Task.WhenAll(tasks)
            .ContinueWith(
                _ =>
                {
                    var end = Stopwatch.GetElapsedTime(start);
                    Log.Info($"Analyze all files completed in {end.TotalSeconds} s.");
                },
                cancellationToken
            );
    }

    public Task AnalyzeFileFromOpenedFileAsync(Uri fileUri)
    {
        if (!gameFilesService.TryGetFileText(fileUri, out string? fileText))
        {
            return Task.CompletedTask;
        }

        return AnalyzeFileAsync(fileUri.ToSystemPath(), fileText);
    }

    private Task AnalyzeFileFromFilePathAsync(string filePath)
    {
        return AnalyzeFileAsync(filePath, File.ReadAllText(filePath));
    }

    private Task AnalyzeFileAsync(string filePath, string fileText)
    {
        if (!TextParser.TryParse(filePath, fileText, out var node, out var error))
        {
            return editorDiagnosisService.AddDiagnoseAsync(error, new Uri(filePath, UriKind.Absolute));
        }

        var gameFileType = GameFileType.FromFilePath(filePath);

        var diagnoses = AnalyzeFile(node, filePath, gameFileType);
        return editorDiagnosisService.AddDiagnoseAsync(
            new PublishDiagnosticsParams { Diagnostics = diagnoses, Uri = filePath }
        );
    }

    private List<Diagnostic> AnalyzeFile(Node node, string filePath, GameFileType gameFileType)
    {
        return gameFileType.Name switch
        {
            nameof(GameFileType.State) => stateAnalyzerService.Analyze(node, filePath),
            nameof(GameFileType.Character) => characterAnalyzerService.Analyze(node),
            _ => EmptyDiagnoses
        };
    }

    private static readonly List<Diagnostic> EmptyDiagnoses = [];
}
