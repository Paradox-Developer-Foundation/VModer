using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentColor;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;

namespace VModer.Core.Services;

public sealed class DocumentColorService(GameFilesService gameFilesService)
{
    private static Task<DocumentColorResponse> Empty => Task.FromResult(new DocumentColorResponse([]));

    public Task<DocumentColorResponse> GetDocumentColorAsync(DocumentColorParams request)
    {
        var filePathUri = request.TextDocument.Uri.Uri;
        if (!gameFilesService.TryGetFileText(filePathUri, out string? fileText))
        {
            return Empty;
        }

        string filePath = filePathUri.ToSystemPath();
        var fileType = GameFileType.FromFilePath(filePath);
        if (fileType == GameFileType.Countries)
        {
            return Task.FromResult(GetColorInCountriesFile(filePath, fileText));
        }

        return Empty;
    }

    private static DocumentColorResponse GetColorInCountriesFile(string filePath, string fileText)
    {
        if (!TextParser.TryParse(filePath, fileText, out var node, out _))
        {
            return new DocumentColorResponse([]);
        }

        var colorsInfo = new List<ColorInformation>();

        Span<float> colors = stackalloc float[3];
        foreach (var child in node.AllArray)
        {
            if (
                !child.TryGetNode(out var colorNode)
                || !colorNode.Key.Equals("color", StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            var colorLeafValues = colorNode.LeafValues.ToArray();
            if (colorLeafValues.Length != 3)
            {
                continue;
            }

            int index = 0;
            foreach (var leafValue in colorLeafValues)
            {
                if (float.TryParse(leafValue.ValueText, out float color))
                {
                    colors[index++] = color;
                }
            }

            colorsInfo.Add(
                new ColorInformation
                {
                    Range = colorNode.Position.ToDocumentRange(),
                    Color = new DocumentColor(colors[0], colors[1], colors[2])
                }
            );
        }

        return new DocumentColorResponse(colorsInfo);
    }
}
