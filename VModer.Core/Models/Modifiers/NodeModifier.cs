using System.Diagnostics;
using ParadoxPower.Process;

namespace VModer.Core.Models.Modifiers;

[DebuggerDisplay("{Key}")]
public sealed class NodeModifier : IModifier
{
    public string Key { get; }
    public IReadOnlyList<LeafModifier> Modifiers { get; }
    public ModifierType Type => ModifierType.Node;

    public NodeModifier(string key, IEnumerable<LeafModifier> modifiers)
    {
        Key = key;
        Modifiers = modifiers.ToArray();
    }
    
    public static NodeModifier FromNode(Node node)
    {
        return new NodeModifier(node.Key, node.Leaves.Select(LeafModifier.FromLeaf));
    }
}
