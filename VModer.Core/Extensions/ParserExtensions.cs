using System.ComponentModel;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using VModer.Core.Models;
using Position = ParadoxPower.Utilities.Position;

namespace VModer.Core.Extensions;

public static class ParserExtensions
{
    public static GameValueType ToLocalValueType(this Types.Value value)
    {
        if (value.IsBool)
        {
            return GameValueType.Bool;
        }

        if (value.IsFloat)
        {
            return GameValueType.Float;
        }

        if (value.IsInt)
        {
            return GameValueType.Int;
        }

        if (value.IsString)
        {
            return GameValueType.String;
        }

        if (value.IsQString)
        {
            return GameValueType.StringWithQuotation;
        }

        // if (value.IsClause)
        // {
        //     return GameValueType.Clause;
        // }
        throw new InvalidEnumArgumentException(nameof(value));
    }

    public static string? GetKeyOrNull(this Child child)
    {
        if (child.TryGetLeaf(out var leaf))
        {
            return leaf.Key;
        }

        if (child.TryGetNode(out var node))
        {
            return node.Key;
        }

        if (child.TryGetLeafValue(out var leafValue))
        {
            return leafValue.Key;
        }

        return null;
    }

    public static DocumentRange ToDocumentRange(this Position.Range position)
    {
        return new DocumentRange(
            new EmmyLua.LanguageServer.Framework.Protocol.Model.Position(
                // VS Code 以 0 开始, 解析器 以 1 开始
                position.StartLine - 1,
                position.StartColumn
            ),
            new EmmyLua.LanguageServer.Framework.Protocol.Model.Position(
                position.EndLine - 1,
                position.EndColumn
            )
        );
    }

    public static bool TryGetIntCast(this Types.Value value, out int result)
    {
        if (value.TryGetInt(out result))
        {
            return true;
        }

        if (value.TryGetDecimal(out decimal decimalValue))
        {
            result = (int)decimalValue;
            return true;
        }

        result = 0;
        return false;
    }
}
