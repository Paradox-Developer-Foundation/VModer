using ParadoxPower.Process;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class OperationsService()
    : CommonResourcesService<OperationsService, string[]>(
        Path.Combine(Keywords.Common, "operations"),
        WatcherFilter.Text
    )
{
    public IEnumerable<string> OperationNames => Resources.Values.SelectMany(names => names);

    protected override string[] ParseFileToContent(Node rootNode)
    {
        var operations = new List<string>();

        foreach (var node in rootNode.Nodes)
        {
            operations.Add(node.Key);
        }

        return operations.ToArray();
    }
}
