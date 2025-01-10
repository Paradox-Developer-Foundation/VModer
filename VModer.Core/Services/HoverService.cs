using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using EmmyLua.LanguageServer.Framework.Protocol.Model;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Markup;
using Markdown;
using MethodTimer;
using NLog;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Parser;
using VModer.Core.Models;
using VModer.Core.Models.Character;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;

namespace VModer.Core.Services;

public sealed class HoverService
{
    private readonly GameFilesService _gameFilesService;
    private readonly ModifierDisplayService _modifierDisplayService;
    private readonly LocalizationService _localizationService;
    private readonly CharacterTraitsService _characterTraitsService;
    private readonly LeaderTraitsService _leaderTraitsService;

    private static readonly string[] GeneralKeywords = ["field_marshal", "corps_commander", "navy_leader"];
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public HoverService(
        GameFilesService gameFilesService,
        ModifierDisplayService modifierDisplayService,
        LocalizationService localizationService,
        CharacterTraitsService characterTraitsService,
        LeaderTraitsService leaderTraitsService
    )
    {
        _gameFilesService = gameFilesService;
        _modifierDisplayService = modifierDisplayService;
        _localizationService = localizationService;
        _characterTraitsService = characterTraitsService;
        _leaderTraitsService = leaderTraitsService;
    }

    [Time]
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

        string hoverText;
        var fileType = GameFileType.FromFilePath(filePath);
        if (fileType == GameFileType.Character)
        {
            hoverText = GetCharacterDisplayText(rootNode, request);
        }
        else
        {
            hoverText = GetModifierDisplayText(rootNode, request);
        }

        return Task.FromResult<HoverResponse?>(
            new HoverResponse
            {
                Contents = new MarkupContent { Kind = MarkupKind.Markdown, Value = hoverText }
            }
        );
    }

    private string GetCharacterDisplayText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var node = FindAdjacentNodeByPosition(rootNode, localPosition);
        string result = string.Empty;

        if (
            Array.Exists(
                GeneralKeywords,
                keyword => keyword.Equals(node.Key, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            result = GetGeneralDisplayText(node);
        }
        else if (node.Key.Equals("advisor", StringComparison.OrdinalIgnoreCase))
        {
            result = GetAdvisorDisplayText(node);
        }

        return result;
    }

    private string GetGeneralDisplayText(Node node)
    {
        var builder = new MarkdownDocument();
        var skillSet = SkillType.List.ToDictionary(
            type => type.Value,
            _ => (ushort)0,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var child in node.AllArray)
        {
            if (child.IsLeafChild)
            {
                var leaf = child.leaf;
                if (skillSet.ContainsKey(leaf.Key) && ushort.TryParse(leaf.ValueText, out ushort value))
                {
                    skillSet[leaf.Key] = value;
                }
            }
            else if (child.IsNodeChild)
            {
                AddGeneralTraits(child.node, builder);
            }
        }

        var skillType = SkillCharacterType.FromCharacterType(node.Key);
        foreach (
            string skillInfo in skillSet.SelectMany(kvp =>
                _modifierDisplayService.GetSkillModifierDescription(
                    SkillType.FromValue(kvp.Key),
                    skillType,
                    kvp.Value
                )
            )
        )
        {
            builder.AppendParagraph(skillInfo);
        }

        return builder.ToString();
    }

    private void AddLeaderTraits(Node node, MarkdownDocument builder)
    {
        AddTraitsDescription(
            node,
            builder,
            traitKey =>
            {
                _leaderTraitsService.TryGetValue(traitKey, out var trait);
                return trait?.Modifiers;
            }
        );
    }

    private void AddGeneralTraits(Node node, MarkdownDocument builder)
    {
        AddTraitsDescription(
            node,
            builder,
            traitKey =>
            {
                _characterTraitsService.TryGetTrait(traitKey, out var trait);
                return trait?.AllModifiers;
            }
        );
    }

    private void AddTraitsDescription(
        Node node,
        MarkdownDocument builder,
        Func<string, IEnumerable<IModifier>?> modifiersFactory
    )
    {
        if (!node.Key.Equals("traits", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var traits = node.LeafValues.Select(trait => trait.Key);
        builder.AppendHeader("特质:", 3);

        foreach (string traitKey in traits)
        {
            builder.AppendListItem(_localizationService.GetValue(traitKey));
            var modifiers = modifiersFactory(traitKey);
            if (modifiers is not null)
            {
                var infos = _modifierDisplayService.GetDescription(modifiers);
                foreach (string info in infos)
                {
                    builder.AppendListItem(info, info.StartsWith("  ") ? 2 : 1);
                }
            }
        }
        builder.AppendHorizontalRule();
    }

    private string GetAdvisorDisplayText(Node node)
    {
        var builder = new MarkdownDocument();

        foreach (var child in node.AllArray)
        {
            if (child.IsNodeChild)
            {
                AddLeaderTraits(child.node, builder);
            }
            else if (child.IsLeafChild)
            {
                var leaf = child.leaf;
                if (leaf.Key.Equals("slot", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendParagraph($"类别: {_localizationService.GetValue(leaf.ValueText)}");
                }
            }
        }

        return builder.ToString();
    }

    private string GetModifierDisplayText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var node = FindAdjacentNodeByPosition(rootNode, localPosition);
        Log.Debug("光标所在 Node, Key:{Key}, Pos: {Pos}", node.Key, localPosition);
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

        var builder = new MarkdownDocument();
        foreach (string modifierInfo in _modifierDisplayService.GetDescription(modifiers))
        {
            builder.AppendParagraph(modifierInfo);
        }

        return builder.ToString();
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
    /// <param name="node">光标所在的 <see cref="Node"/>, 使用 <see cref="FindAdjacentNodeByPosition"/> 方法获取</param>
    /// <param name="cursorPosition">光标位置(以 1 开始)</param>
    /// <returns></returns>
    private static Child FindChildByPosition(Node node, Position cursorPosition)
    {
        if (node.Position.StartLine == cursorPosition.Line)
        {
            return Child.NewNodeChild(node);
        }

        foreach (var child in node.AllArray)
        {
            var childPosition = child.Position;
            if (cursorPosition.Line > childPosition.StartLine && cursorPosition.Line < childPosition.EndLine)
            {
                return child;
            }

            if (
                (
                    cursorPosition.Line == childPosition.StartLine
                    && cursorPosition.Character >= childPosition.StartColumn
                )
                || (
                    cursorPosition.Line == childPosition.EndLine
                    && cursorPosition.Character <= childPosition.EndColumn
                )
            )
            {
                return child;
            }
        }

        return Child.NewNodeChild(node);
    }

    /// <summary>
    /// 获取离光标最近的 <see cref="Node"/> (即容纳光标的上级节点, 当光标放在节点上时返回此节点)
    /// </summary>
    /// <param name="node">节点</param>
    /// <param name="cursorPosition">光标位置(以 1 开始)</param>
    /// <returns>离光标最近的 <see cref="Node"/></returns>
    private static Node FindAdjacentNodeByPosition(Node node, Position cursorPosition)
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
}
