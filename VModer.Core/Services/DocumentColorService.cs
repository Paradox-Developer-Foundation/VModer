using System.Text;
using EmmyLua.LanguageServer.Framework.Protocol.Message.DocumentColor;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using MethodTimer;
using NLog;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;

namespace VModer.Core.Services;

public sealed class DocumentColorService(GameFilesService gameFilesService)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ColorPresentationResponse GetColorPresentation(ColorPresentationParams request)
    {
        double red = request.Color.Red * 255;
        double green = request.Color.Green * 255;
        double blue = request.Color.Blue * 255;
        var (h, s, v) = RgbToHsv(red, green, blue);

        List<ColorPresentation> presentations =
        [
            GetRgbColorPresentation(red, green, blue, request.Range!.Value),
            GetHsvColorPresentation(h, s, v, request.Range!.Value)
        ];

        return new ColorPresentationResponse(presentations);
    }

    /// <summary>
    /// 获取这段位置上的文字
    /// </summary>
    /// <param name="text"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    private string GetTextByPosition(string text, DocumentRange position)
    {
        var start = position.Start;
        var end = position.End;
        int lineCount = Math.Max(1, end.Line - start.Line);

        int currentLine = 0;
        var sb = new StringBuilder();
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            if (currentLine < start.Line)
            {
                currentLine++;
                continue;
            }

            int endCharacter = Math.Min(line.Length, end.Character);
            if (currentLine == start.Line && start.Line == end.Line)
            {
                sb.Append(line[start.Character..endCharacter]);
            }
            else if (currentLine == start.Line)
            {
                sb.Append(line[start.Character..]);
            }
            else if (currentLine == end.Line)
            {
                sb.Append(line[..endCharacter]);
            }
            else
            {
                sb.Append(line);
            }
            if (--lineCount <= 0)
            {
                break;
            }

            currentLine++;
        }

        return sb.ToString();
    }

    private static ColorPresentation GetRgbColorPresentation(
        double red,
        double green,
        double blue,
        DocumentRange documentRange
    )
    {
        return new ColorPresentation
        {
            Label = $"rgb({red:F0}, {green:F0}, {blue:F0})",
            TextEdit = new TextEdit
            {
                NewText = $"color = rgb {{ {red:F0} {green:F0} {blue:F0} }}",
                Range = documentRange
            }
        };
    }

    private static ColorPresentation GetHsvColorPresentation(
        double h,
        double s,
        double v,
        DocumentRange documentRange
    )
    {
        return new ColorPresentation
        {
            Label = $"hsv({h:F0}, {s:P}, {v:P})",
            TextEdit = new TextEdit
            {
                NewText = $"color = HSV {{ {h / 360.0:F2} {s:F2} {v:F2} }}",
                Range = documentRange
            }
        };
    }

    private static (double H, double S, double V) RgbToHsv(double red, double green, double blue)
    {
        double r = red / 255.0;
        double g = green / 255.0;
        double b = blue / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h;
        if (Math.Abs(delta) < 1e-6)
        {
            h = 0;
        }
        else if (Math.Abs(max - r) < 1e-6)
        {
            h = 60.0 * (((g - b) / delta) % 6);
        }
        else if (Math.Abs(max - g) < 1e-6)
        {
            h = 60.0 * (((b - r) / delta) + 2);
        }
        else
        {
            h = 60.0 * (((r - g) / delta) + 4);
        }

        if (h < 0)
        {
            h += 360.0;
        }

        double s = Math.Abs(max) < 1e-6 ? 0 : (delta / max);
        double v = max;

        return (h, s, v);
    }

    [Time("获取颜色选择器位置")]
    public Task<DocumentColorResponse> GetDocumentColorAsync(DocumentColorParams request)
    {
        var filePathUri = request.TextDocument.Uri.Uri;
        if (!gameFilesService.TryGetFileText(filePathUri, out string? fileText))
        {
            return Task.FromResult(new DocumentColorResponse([]));
        }

        string filePath = filePathUri.ToSystemPath();
        var fileType = GameFileType.FromFilePath(filePath);
        DocumentColorResponse? colorsResponse = null;
        if (fileType == GameFileType.Countries)
        {
            colorsResponse = GetDocumentColorForCountriesFolder(filePath, fileText);
        }

        if (fileType == GameFileType.CoreGfx)
        {
            colorsResponse = GetDocumentColorForCoreGfx(filePath, fileText);
        }

        if (fileType == GameFileType.Ideologies)
        {
            colorsResponse = GetDocumentColorForIdeologies(filePath, fileText);
        }

        return Task.FromResult(colorsResponse ?? new DocumentColorResponse([]));
    }

    private static DocumentColorResponse GetDocumentColorForCoreGfx(string filePath, string fileText)
    {
        if (!TextParser.TryParse(filePath, fileText, out var rootNode, out _))
        {
            return new DocumentColorResponse([]);
        }

        var colors = new List<ColorInformation>();
        AddTextColors(rootNode, colors);

        return new DocumentColorResponse(colors);
    }

    private static void AddTextColors(Node node, List<ColorInformation> colorsInfo)
    {
        foreach (var childNode in node.Nodes)
        {
            if (childNode.Key.Equals("textcolors", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var colorNode in childNode.Nodes)
                {
                    AddColorInfoToList(colorNode, colorsInfo);
                }
            }
            // 确保不是 LeafValues 节点以避免无效的递归调用
            else if (!childNode.LeafValues.Any())
            {
                AddTextColors(childNode, colorsInfo);
            }
        }
    }

    private static DocumentColorResponse GetDocumentColorForIdeologies(string filePath, string fileText)
    {
        if (!TextParser.TryParse(filePath, fileText, out var rootNode, out _))
        {
            return new DocumentColorResponse([]);
        }

        var colorsInfo = new List<ColorInformation>();
        foreach (
            var ideologiesNode in rootNode.Nodes.Where(node =>
                node.Key.Equals("ideologies", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            foreach (var ideologyNode in ideologiesNode.Nodes)
            {
                foreach (
                    var colorNode in ideologyNode.Nodes.Where(node =>
                        node.Key.Equals("color", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    AddColorInfoToList(colorNode, colorsInfo);
                }
            }
        }

        return new DocumentColorResponse(colorsInfo);
    }

    private static DocumentColorResponse GetDocumentColorForCountriesFolder(string filePath, string fileText)
    {
        if (!TextParser.TryParse(filePath, fileText, out var rootNode, out _))
        {
            return new DocumentColorResponse([]);
        }

        string fileName = Path.GetFileName(filePath);
        var colorsInfo =
            fileName.Equals("colors.txt", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("cosmetic.txt", StringComparison.OrdinalIgnoreCase)
                ? GetColorInNodeColorFile(rootNode)
                : GetColorInLeafColorFile(rootNode);

        return new DocumentColorResponse(colorsInfo);
    }

    private static List<ColorInformation> GetColorInNodeColorFile(Node rootNode)
    {
        var colorsInfo = new List<ColorInformation>();

        foreach (var countryNode in rootNode.Nodes)
        {
            foreach (
                var colorNode in countryNode.Nodes.Where(node =>
                    node.Key.Equals("color", StringComparison.OrdinalIgnoreCase)
                    || node.Key.Equals("color_ui", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                AddColorInfoToList(colorNode, colorsInfo);
            }
        }

        return colorsInfo;
    }

    private static List<ColorInformation> GetColorInLeafColorFile(Node rootNode)
    {
        var colorsInfo = new List<ColorInformation>();

        foreach (
            var colorNode in rootNode.Nodes.Where(node =>
                node.Key.Equals("color", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            AddColorInfoToList(colorNode, colorsInfo);
        }

        return colorsInfo;
    }

    private static void AddColorInfoToList(Node colorNode, List<ColorInformation> colorsInfo)
    {
        Span<double> colors = stackalloc double[3];
        var colorLeafValues = colorNode.LeafValues.ToArray();
        if (colorLeafValues.Length != 3)
        {
            return;
        }

        int index = 0;
        foreach (var leafValue in colorLeafValues)
        {
            if (double.TryParse(leafValue.ValueText, out double color))
            {
                colors[index++] = color;
            }
        }

        colorsInfo.Add(
            new ColorInformation
            {
                Range = colorNode.Position.ToDocumentRange(),
                Color = GetColor(colors, colorNode)
            }
        );
    }

    private static DocumentColor GetColor(Span<double> colors, Node colorNode)
    {
        if (
            colorNode.Leaves.Count == 0
            || colorNode.Leaves.Any(leaf => leaf.Key.Equals("rgb", StringComparison.OrdinalIgnoreCase))
        )
        {
            return new DocumentColor(colors[0] / 255, colors[1] / 255, colors[2] / 255, 255);
        }

        var (r, g, b) = HsvToRgb(colors[0], colors[1], colors[2]);
        return new DocumentColor(r, g, b, 255);
    }

    private static (double r, double g, double b) HsvToRgb(double h, double s, double v)
    {
        double angle = h * 360.0;
        double c = v * s;
        double x = c * (1.0 - Math.Abs((angle / 60.0) % 2 - 1));
        double m = v - c;

        double r,
            g,
            b;
        if (angle < 60)
        {
            r = c;
            g = x;
            b = 0;
        }
        else if (angle < 120)
        {
            r = x;
            g = c;
            b = 0;
        }
        else if (angle < 180)
        {
            r = 0;
            g = c;
            b = x;
        }
        else if (angle < 240)
        {
            r = 0;
            g = x;
            b = c;
        }
        else if (angle < 300)
        {
            r = x;
            g = 0;
            b = c;
        }
        else
        {
            r = c;
            g = 0;
            b = x;
        }

        r += m;
        g += m;
        b += m;
        return (r, g, b);
    }
}
