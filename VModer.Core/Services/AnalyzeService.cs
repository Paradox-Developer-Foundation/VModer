using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using NLog;
using ParadoxPower.CSharp;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Analyzers;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Languages;

namespace VModer.Core.Services;

public sealed class AnalyzeService(
    GameFilesService gameFilesService,
    StateAnalyzerService stateAnalyzerService,
    CharacterAnalyzerService characterAnalyzerService,
    SettingsService settingsService
)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public List<Task<(string FilePath, List<Diagnostic> Diagnostics)>> AnalyzeAllFilesAsync(
        CancellationToken cancellationToken
    )
    {
        var tasks = new List<Task<(string FilePath, List<Diagnostic> Diagnostics)>>();

        foreach (
            string filePath in Directory.EnumerateFiles(
                settingsService.ModRootFolderPath,
                "*.txt",
                SearchOption.AllDirectories
            )
        )
        {
            tasks.Add(Task.Run(() => (filePath, AnalyzeFileFromFilePath(filePath)), cancellationToken));
        }

        return tasks;
    }

    public List<Diagnostic> AnalyzeFileFromOpenedFile(Uri fileUri)
    {
        string filePath = fileUri.ToSystemPath();
        if (settingsService.AnalysisBlackList.Contains(Path.GetFileName(filePath)))
        {
            return [];
        }

        if (!gameFilesService.TryGetFileText(fileUri, out string? fileText))
        {
            return [];
        }

        int textSize = fileText.Length * 2;
        if (textSize > settingsService.ParseFileMaxBytesSize)
        {
            return [];
        }

        return AnalyzeFile(filePath, fileText);
    }

    private List<Diagnostic> AnalyzeFileFromFilePath(string filePath)
    {
        if (settingsService.AnalysisBlackList.Contains(Path.GetFileName(filePath)))
        {
            return [];
        }

        if (new FileInfo(filePath).Length > settingsService.ParseFileMaxBytesSize)
        {
            return [];
        }

        return AnalyzeFile(filePath, File.ReadAllText(filePath));
    }

    private List<Diagnostic> AnalyzeFile(string filePath, string fileText)
    {
        if (!TextParser.TryParse(filePath, fileText, out var rootNode, out var error))
        {
            return
            [
                new Diagnostic
                {
                    Code = ErrorCode.VM1000,
                    Range = GetRange(error, 0),
                    Message = error.ErrorMessage,
                    Severity = DiagnosticSeverity.Error
                }
            ];
        }

        var gameFileType = GameFileType.FromFilePath(filePath);

        var diagnoses = AnalyzeFile(rootNode, filePath, gameFileType);
        if (gameFileType != GameFileType.Ideologies && gameFileType != GameFileType.Modifiers)
        {
            AnalyzeHelper.AnalyzeEmptyNode(rootNode, diagnoses);
        }

        return diagnoses;
    }

    private static DocumentRange GetRange(ParserError position, int length)
    {
        int startChar;
        int endChar;
        int startLine = Math.Max((int)position.Line - 1, 0);
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

        return new DocumentRange(start, end);
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
}
