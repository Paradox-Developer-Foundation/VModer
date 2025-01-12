using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class LeaderTraitsService()
    : CommonResourcesService<LeaderTraitsService, FrozenDictionary<string, LeaderTrait>>(
        Path.Combine([Keywords.Common, "country_leader"]),
        WatcherFilter.Text
    )
{
    private Dictionary<string, FrozenDictionary<string, LeaderTrait>>.ValueCollection Traits =>
        Resources.Values;

    public bool TryGetValue(string traitName, [NotNullWhen(true)] out LeaderTrait? trait)
    {
        foreach (var traitDict in Traits)
        {
            if (traitDict.TryGetValue(traitName, out trait))
            {
                return true;
            }
        }

        trait = null;
        return false;
    }

    protected override FrozenDictionary<string, LeaderTrait> ParseFileToContent(Node rootNode)
    {
        var leaderTraits = new Dictionary<string, LeaderTrait>();
        foreach (
            var leaderTraitsNode in rootNode.Nodes.Where(node =>
                node.Key.Equals("leader_traits", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            foreach (var traitNode in leaderTraitsNode.Nodes)
            {
                leaderTraits[traitNode.Key] = ParseTraitToLeaderTrait(traitNode);
            }
        }

        return leaderTraits.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static readonly string[] TraitFeatureLeafKeywords = ["sprite", "random", "command_cap"];

    // 仅处理数组中存在的节点修饰符
    // https://hoi4.paradoxwikis.com/Character_modding#Country_leader_traits
    private static readonly string[] TraitModifierNodeKeywords = ["targeted_modifier", "equipment_bonus"];

    private static LeaderTrait ParseTraitToLeaderTrait(Node traitNode)
    {
        var modifiers = new List<IModifier>(4);
        foreach (var child in traitNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (
                    Array.Exists(
                        TraitFeatureLeafKeywords,
                        keyword => keyword.Equals(leaf.Key, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    continue;
                }

                modifiers.Add(LeafModifier.FromLeaf(leaf));
            }
            else if (child.TryGetNode(out var node))
            {
                if (
                    !Array.Exists(
                        TraitModifierNodeKeywords,
                        keyword => keyword.Equals(node.Key, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    continue;
                }

                modifiers.Add(NodeModifier.FromNode(node));
            }
        }

        return new LeaderTrait(traitNode.Key, modifiers);
    }
}
