using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;

namespace VModer.Core.Services;

public sealed class AnalyzeService
{
    private readonly GameFilesService _gameFilesService;
    private readonly EditorDiagnosisService _editorDiagnosisService;

    public AnalyzeService(GameFilesService gameFilesService, EditorDiagnosisService editorDiagnosisService)
    {
        _gameFilesService = gameFilesService;
        _editorDiagnosisService = editorDiagnosisService;
    }

    public Task AnalyzeFileAsync(Uri fileUri)
    {
        if (!_gameFilesService.TryGetFileText(fileUri, out string? fileText))
        {
            return Task.CompletedTask;
        }

        string filePath = fileUri.ToSystemPath();
        if (TextParser.TryParse(filePath, fileText, out _, out var error))
        {
            return _editorDiagnosisService.ClearDiagnoseAsync(fileUri);
        }

        return _editorDiagnosisService.AddDiagnoseAsync(error, fileUri);
    }
}
