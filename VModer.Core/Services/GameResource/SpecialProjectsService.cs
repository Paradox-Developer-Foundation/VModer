using ParadoxPower.Process;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class SpecialProjectsService()
    : CommonResourcesService<SpecialProjectsService, string[]>(
        Path.Combine(Keywords.Common, "special_projects", "projects"),
        WatcherFilter.Text
    )
{
    public IEnumerable<string> SpecialProjectNames => Resources.Values.SelectMany(names => names);

    protected override string[] ParseFileToContent(Node rootNode)
    {
        var specialProjects = new List<string>();

        foreach (var node in rootNode.Nodes)
        {
            specialProjects.Add(node.Key);
        }

        return specialProjects.ToArray();
    }
}
