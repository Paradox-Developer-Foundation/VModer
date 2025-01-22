using ParadoxPower.Process;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class IdeologiesService()
    : CommonResourcesService<IdeologiesService, string[]>(
        Path.Combine(Keywords.Common, "ideologies"),
        WatcherFilter.Text
    )
{
    protected override string[] ParseFileToContent(Node rootNode)
    {
        var ideologies = new List<string>();
        foreach (
            var ideologiesNode in rootNode.Nodes.Where(node =>
                node.Key.Equals("ideologies", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            foreach (var ideologyNode in ideologiesNode.Nodes)
            {
                var typeNode = ideologyNode.Nodes.FirstOrDefault(node =>
                    node.Key.Equals("type", StringComparison.OrdinalIgnoreCase)
                );
                if (typeNode is null)
                {
                    continue;
                }

                ideologies.AddRange(typeNode.Nodes.Select(node => node.Key));
            }
        }

        return ideologies.ToArray();
    }
}
