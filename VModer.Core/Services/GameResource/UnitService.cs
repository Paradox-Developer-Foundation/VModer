using System.Collections.Frozen;
using ParadoxPower.Process;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class UnitService()
    : CommonResourcesService<UnitService, FrozenSet<string>>(
        Path.Combine(Keywords.Common, "units"),
        WatcherFilter.Text
    )
{
    private ICollection<FrozenSet<string>> Units => Resources.Values;

    /// <summary>
    /// 判断是否是游戏内兵种
    /// </summary>
    /// <param name="unitName">兵种名</param>
    public bool Contains(string unitName)
    {
        foreach (var unitSet in Units)
        {
            if (unitSet.Contains(unitName))
            {
                return true;
            }
        }

        return false;
    }

    protected override FrozenSet<string> ParseFileToContent(Node rootNode)
    {
        var unitSet = new HashSet<string>(8, StringComparer.OrdinalIgnoreCase);

        foreach (
            var subUnitNode in rootNode.Nodes.Where(node =>
                node.Key.Equals("sub_units", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            foreach (var unitNode in subUnitNode.Nodes)
            {
                unitSet.Add(unitNode.Key);
            }
        }

        return unitSet.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
