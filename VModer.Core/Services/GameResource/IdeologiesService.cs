using ParadoxPower.Process;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class IdeologiesService()
    : CommonResourcesService<IdeologiesService, string[]>(
        Path.Combine(Keywords.Common, "ideologies"),
        WatcherFilter.Text
    )
{
    public IEnumerable<string> All => Ideologies.SelectMany(ideologies => ideologies);

    private ICollection<string[]> Ideologies => Resources.Values;

    protected override string[] ParseFileToContent(Node rootNode)
    {
        var ideologies = new List<string>(4);
        foreach (
            var ideologiesNode in rootNode.Nodes.Where(node =>
                node.Key.Equals("ideologies", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            ideologies.AddRange(ideologiesNode.Nodes.Select(ideologyNode => ideologyNode.Key));
        }

        return ideologies.ToArray();
    }
}
