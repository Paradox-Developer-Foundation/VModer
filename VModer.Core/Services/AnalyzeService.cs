using System.Diagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Analyzers;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Languages;

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
        string filePath = fileUri.ToSystemPath();
        if (settingsService.AnalysisBlackList.Contains(Path.GetFileName(filePath)))
        {
            return Task.CompletedTask;
        }

        if (!gameFilesService.TryGetFileText(fileUri, out string? fileText))
        {
            return Task.CompletedTask;
        }

        int textSize = fileText.Length * 2;
        if (textSize > settingsService.ParseFileMaxBytesSize)
        {
            return Task.CompletedTask;
        }

        return AnalyzeFileAsync(filePath, fileText);
    }

    private Task AnalyzeFileFromFilePathAsync(string filePath)
    {
        if (settingsService.AnalysisBlackList.Contains(Path.GetFileName(filePath)))
        {
            return Task.CompletedTask;
        }

        if (new FileInfo(filePath).Length > settingsService.ParseFileMaxBytesSize)
        {
            return Task.CompletedTask;
        }

        return AnalyzeFileAsync(filePath, File.ReadAllText(filePath));
    }

    private Task AnalyzeFileAsync(string filePath, string fileText)
    {
        if (!TextParser.TryParse(filePath, fileText, out var rootNode, out var error))
        {
            return editorDiagnosisService.AddDiagnoseAsync(error, new Uri(filePath, UriKind.Absolute));
        }

        var gameFileType = GameFileType.FromFilePath(filePath);

        var diagnoses = AnalyzeFile(rootNode, filePath, gameFileType);
        if (gameFileType != GameFileType.Ideologies && gameFileType != GameFileType.Modifiers)
        {
            AnalyzeEmptyNode(rootNode, diagnoses);
        }

        return editorDiagnosisService.AddDiagnoseAsync(
            new PublishDiagnosticsParams { Diagnostics = diagnoses, Uri = filePath }
        );
    }

    private List<Diagnostic> AnalyzeFile(Node rootNode, string filePath, GameFileType gameFileType)
    {
        return gameFileType.Name switch
        {
            nameof(GameFileType.State) => stateAnalyzerService.Analyze(rootNode, filePath),
            nameof(GameFileType.Character) => characterAnalyzerService.Analyze(rootNode),
            _ => []
        };
    }

    private static void AnalyzeEmptyNode(Node node, List<Diagnostic> list)
    {
        foreach (var childNode in node.Nodes)
        {
            if (childNode.AllArray.Length == 0)
            {
                list.Add(
                    new Diagnostic
                    {
                        Range = childNode.Position.ToDocumentRange(),
                        Code = ErrorCode.VM1005,
                        Message = Resources.Analyzer_UnusedStatement,
                        Severity = DiagnosticSeverity.Warning,
                        Tags = [DiagnosticTag.Unnecessary]
                    }
                );
            }
            else
            {
                AnalyzeEmptyNode(childNode, list);
            }
        }
    }
}
