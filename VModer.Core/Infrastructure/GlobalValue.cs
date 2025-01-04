using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using Position = ParadoxPower.Utilities.Position;

namespace VModer.Core.Infrastructure;

public sealed class GlobalValue(string leafKey)
{
    private readonly ConcurrentDictionary<int, string> _values = [];

    public bool TryAdd(
        LeafValue leafValue,
        string filePath,
        [NotNullWhen(false)] out RepetitiveValueInfo? repetitiveValueInfo
    )
    {
        return TryAdd(leafValue.Value, leafValue.Position, filePath, out repetitiveValueInfo);
    }

    public bool TryAdd(
        Node node,
        string filePtah,
        [NotNullWhen(false)] out RepetitiveValueInfo? repetitiveValueInfo
    )
    {
        if (!node.TryGetLeaf(leafKey, out var leaf))
        {
            repetitiveValueInfo = new RepetitiveValueInfo(string.Empty, default, default);
            return false;
        }

        return TryAdd(leaf.Value, leaf.Position, filePtah, out repetitiveValueInfo);
    }

    private bool TryAdd(
        Types.Value parserValue,
        Position.Range position,
        string filePath,
        [NotNullWhen(false)] out RepetitiveValueInfo? repetitiveValueInfo
    )
    {
        repetitiveValueInfo = null;
        if (!parserValue.IsInt)
        {
            repetitiveValueInfo = new RepetitiveValueInfo(string.Empty, position.ToDocumentRange(), default);
            return false;
        }

        if (!int.TryParse(parserValue.ToRawString(), out int value))
        {
            repetitiveValueInfo = new RepetitiveValueInfo(string.Empty, position.ToDocumentRange(), default);
            return false;
        }

        if (_values.TryGetValue(value, out string? path) && filePath != path)
        {
            repetitiveValueInfo = new RepetitiveValueInfo(path, position.ToDocumentRange(), value);
            return false;
        }

        _values.TryAdd(value, filePath);
        return true;
    }
}
