using System.Diagnostics.CodeAnalysis;
using System.Text;
using ParadoxPower.CSharp;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;

namespace VModer.Core.Infrastructure.Parser;

public sealed class TextParser
{
    public string FilePath { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    private readonly ParserError? _error;

    private readonly Node? _node;

    /// <summary>
    /// 构造解析器
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="FileNotFoundException">如果文件不存在</exception>
    /// <exception cref="IOException"></exception>
    private TextParser(string filePath)
        : this(filePath, File.ReadAllText(filePath)) { }

    private TextParser(string filePath, string fileText)
    {
        FilePath = File.Exists(filePath)
            ? filePath
            : throw new FileNotFoundException($"找不到文件: {filePath}", filePath);
        string fileName = Path.GetFileName(filePath);
        var result = Parsers.ParseScriptFile(fileName, fileText);
        IsSuccess = result.IsSuccess;
        if (IsFailure)
        {
            _error = result.GetError();
            return;
        }

        _node = Parsers.ProcessStatements(fileName, filePath, result.GetResult());
    }

    public static bool TryParse(
        string filePath,
        [NotNullWhen(true)] out Node? rootNode,
        [NotNullWhen(false)] out ParserError? error
    )
    {
        return TryParse(filePath, File.ReadAllText(filePath), out rootNode, out error);
    }

    public static bool TryParse(
        string filePath,
        string fileText,
        [NotNullWhen(true)] out Node? rootNode,
        [NotNullWhen(false)] out ParserError? error
    )
    {
        try
        {
            var parser = new TextParser(filePath, fileText);
            if (parser.IsFailure)
            {
                rootNode = null;
                error = parser.GetError();
                return false;
            }

            rootNode = parser.GetResult();
            ProcessConstants(rootNode);

            error = null;
            return true;
        }
        catch (Exception e)
        {
            rootNode = null;
            error = new ParserError(Path.GetFileName(filePath), 0, 0, e.Message);
            return false;
        }
    }

    private static void ProcessConstants(Node rootNode)
    {
        var constants = FindAllDefinedConstants(rootNode);
        ReplaceConstants(rootNode, constants);
    }

    private static Dictionary<string, Types.Value> FindAllDefinedConstants(Node rootNode)
    {
        var constants = new Dictionary<string, Types.Value>();
        foreach (var child in rootNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (leaf.Key.StartsWith('@'))
                {
                    constants[leaf.Key] = leaf.Value;
                }
            }
        }

        return constants;
    }

    private static void ReplaceConstants(Node node, Dictionary<string, Types.Value> constants)
    {
        foreach (var child in node.AllArray)
        {
            if (child.TryGetLeaf(out var leaf) && constants.TryGetValue(leaf.ValueText, out var constant))
            {
                leaf.Value = constant;
            }
            else if (
                child.TryGetLeafValue(out var leafValue) && constants.TryGetValue(leafValue.ValueText, out constant)
            )
            {
                leafValue.Value = constant;
            }
            else if (child.TryGetNode(out var n))
            {
                ReplaceConstants(n, constants);
            }
        }
    }

    static TextParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Node GetResult()
    {
        return _node ?? throw new InvalidOperationException($"文件解析失败, 无法返回解析结果, 文件路径: {FilePath}.");
    }

    public ParserError GetError()
    {
        return _error ?? throw new InvalidOperationException();
    }
}
