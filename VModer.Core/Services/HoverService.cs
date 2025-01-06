using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Services;

public sealed class HoverService
{
    private readonly GameFilesService _gameFilesService;
    private ModifierDisplayService _modifierDisplayService;

    public HoverService(GameFilesService gameFilesService, ModifierDisplayService modifierDisplayService)
    {
        _gameFilesService = gameFilesService;
        _modifierDisplayService = modifierDisplayService;
    }

    public Task<HoverResponse?> GetHoverResponseAsync(HoverParams request)
    {
        string filePath = request.TextDocument.Uri.Uri.ToSystemPath();
        if (!_gameFilesService.TryGetFileText(request.TextDocument.Uri.Uri, out string? text))
        {
            return Task.FromResult<HoverResponse?>(null);
        }
        if (!TextParser.TryParse(filePath, text, out var rootNode, out _))
        {
            return Task.FromResult<HoverResponse?>(null);
        }

        string value = GetModifierDisplayText(rootNode, request);

        return Task.FromResult<HoverResponse?>(
            new HoverResponse
            {
                Contents = new MarkupContent { Kind = MarkupKind.PlainText, Value = value }
            }
        );
    }

    private string GetModifierDisplayText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var node = FindNodeByPosition(rootNode, localPosition);
        if (!node.Key.Equals("modifier", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var child = FindChildByPosition(node, localPosition);
        IEnumerable<IModifier> modifiers = [];
        if (child.IsNodeChild)
        {
            modifiers = GetModifiersForNode(child.node);
        }
        else if (child.IsLeafChild)
        {
            modifiers = [LeafModifier.FromLeaf(child.leaf)];
        }

        return string.Join('\n', _modifierDisplayService.GetDescription(modifiers));
    }

    private static List<IModifier> GetModifiersForNode(Node node)
    {
        var modifiers = new List<IModifier>();
        foreach (var child in node.AllArray)
        {
            if (child.IsLeafChild)
            {
                modifiers.Add(LeafModifier.FromLeaf(child.leaf));
            }
            else if (child.IsNodeChild)
            {
                modifiers.Add(NodeModifier.FromNode(child.node));
            }
        }

        return modifiers;
    }

    /// <summary>
    /// 获取光标指向的 <see cref="Child"/>
    /// </summary>
    /// <param name="node">光标所在的 <see cref="Node"/>, 使用 <see cref="FindNodeByPosition"/> 方法获取</param>
    /// <param name="cursorPosition"></param>
    /// <returns></returns>
    private static Child FindChildByPosition(Node node, Position cursorPosition)
    {
        if (node.Position.StartLine == cursorPosition.Line)
        {
            return Child.NewNodeChild(node);
        }

        foreach (var child in node.AllArray)
        {
            if (
                child.Position.StartLine == cursorPosition.Line
                || child.Position.EndLine == cursorPosition.Line
            )
            {
                return child;
            }
        }

        return Child.NewNodeChild(node);
    }

    /// <summary>
    /// 获取离光标最近的 <see cref="Node"/> (即容纳光标的上级节点)
    /// </summary>
    /// <param name="node">节点</param>
    /// <param name="cursorPosition">光标位置(以 1 开始)</param>
    /// <returns>离光标最近的 <see cref="Node"/></returns>
    private static Node FindNodeByPosition(Node node, Position cursorPosition)
    {
        foreach (var child in node.Nodes)
        {
            var childPosition = child.Position;
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
                return child;
            }

            if (cursorPosition.Line > childPosition.StartLine && cursorPosition.Line < childPosition.EndLine)
            {
                return FindNodeByPosition(child, cursorPosition);
            }
        }

        return node;
    }
}
