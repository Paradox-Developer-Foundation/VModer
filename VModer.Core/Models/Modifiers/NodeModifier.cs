using System.Diagnostics;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace VModer.Core.Models.Modifiers;

[DebuggerDisplay("{Key}")]
public sealed class NodeModifier : IModifier
{
    public string Key { get; }
    public IReadOnlyList<IModifier> Modifiers { get; }
    public IEnumerable<LeafModifier> Leaves =>
        Modifiers.Where(x => x.Type == ModifierType.Leaf).Select(modifier => (LeafModifier)modifier);
    public ModifierType Type => ModifierType.Node;

    public NodeModifier(string key, IEnumerable<IModifier> modifiers)
    {
        Key = key;
        Modifiers = modifiers.ToArray();
    }

    public static NodeModifier FromNode(Node node)
    {
        var modifiers = new List<IModifier>();
        foreach (var child in node.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                modifiers.Add(LeafModifier.FromLeaf(leaf));
            }
            else if (child.TryGetNode(out var childNode))
            {
                modifiers.Add(FromNode(childNode));
            }
        }
        return new NodeModifier(node.Key, modifiers);
    }
}
