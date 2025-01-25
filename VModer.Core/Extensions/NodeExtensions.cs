using EmmyLua.LanguageServer.Framework.Protocol.Model;
using ParadoxPower.Process;
using VModer.Core.Models;

namespace VModer.Core.Extensions;

public static class NodeExtensions
{
    /// <summary>
    /// 获取离光标最近的 <see cref="Node"/> (即容纳光标的上级节点, 当光标放在节点上时返回此节点)
    /// </summary>
    /// <param name="node">节点</param>
    /// <param name="cursorPosition">光标位置(以 1 开始)</param>
    /// <returns>离光标最近的 <see cref="Node"/></returns>
    public static Node FindAdjacentNodeByPosition(this Node node, LocalPosition cursorPosition)
    {
        foreach (var childNode in node.Nodes)
        {
            var childPosition = childNode.Position;
            if (
                (
                    cursorPosition.Line == childPosition.StartLine
                    && cursorPosition.Character > childPosition.StartColumn
                )
                || (
                    cursorPosition.Line == childPosition.EndLine
                    && cursorPosition.Character < childPosition.EndColumn
                )
            )
            {
                return childNode;
            }

            if (cursorPosition.Line > childPosition.StartLine && cursorPosition.Line < childPosition.EndLine)
            {
                return FindAdjacentNodeByPosition(childNode, cursorPosition);
            }
        }

        return node;
    }

    /// <summary>
    /// 获取光标指向的 <see cref="Child"/>
    /// </summary>
    /// <param name="node">光标所在的 <see cref="Node"/>, 使用 <see cref="FindAdjacentNodeByPosition"/> 方法获取</param>
    /// <param name="cursorPosition">光标位置(以 1 开始)</param>
    /// <returns></returns>
    public static Child FindPointedChildByPosition(this Node node, LocalPosition cursorPosition)
    {
        foreach (var child in node.AllArray)
        {
            var childPosition = child.Position;
            if (cursorPosition.Line > childPosition.StartLine && cursorPosition.Line < childPosition.EndLine)
            {
                return child;
            }

            if (
                cursorPosition.Line == childPosition.StartLine
                && (
                    (
                        cursorPosition.Line == childPosition.EndLine
                        && cursorPosition.Character >= childPosition.StartColumn
                        && cursorPosition.Character <= childPosition.EndColumn
                    )
                    || (
                        cursorPosition.Character >= childPosition.StartColumn
                        && cursorPosition.Line != childPosition.EndLine
                    )
                )
            )
            {
                return child;
            }

            if (
                cursorPosition.Line == childPosition.EndLine
                && cursorPosition.Character <= childPosition.EndColumn
                && cursorPosition.Line != childPosition.StartLine
            )
            {
                return child;
            }
        }

        return Child.Create(node);
    }
}
