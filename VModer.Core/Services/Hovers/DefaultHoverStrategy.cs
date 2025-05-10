using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Helpers;
using VModer.Core.Models;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.Core.Services.Hovers;

public sealed class DefaultHoverStrategy : IHoverStrategy
{
    private readonly ModifierHoverStrategy _modifierHoverStrategy;
    private readonly LocalizationFormatService _localizationFormatService;

    public DefaultHoverStrategy(
        IServiceProvider serviceProvider,
        LocalizationFormatService localizationFormatService
    )
    {
        _modifierHoverStrategy = (ModifierHoverStrategy)
            serviceProvider.GetRequiredKeyedService<IHoverStrategy>(nameof(ModifierHoverStrategy));
        _localizationFormatService = localizationFormatService;
    }

    public GameFileType FileType => GameFileType.Unknown;

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var node = rootNode.FindAdjacentNodeByPosition(localPosition);

        if (ModifierHelper.IsModifierNode(node, request))
        {
            return _modifierHoverStrategy.GetHoverText(rootNode, request);
        }

        var child = node.FindPointedChildByPosition(localPosition);

        if (child.TryGetLeaf(out var leaf) && leaf.Key.EqualsIgnoreCase("name"))
        {
            if (_localizationFormatService.TryGetFormatText(leaf.ValueText, out string? value))
            {
                return value;
            }
        }

        return string.Empty;
    }
}
