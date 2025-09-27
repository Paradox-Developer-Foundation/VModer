using System.Diagnostics.CodeAnalysis;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class StatesProvincesMapService : CommonResourcesService<StatesProvincesMapService, int[]>
{
    private const string StatesKeyword = "states";

    public StatesProvincesMapService()
        : base(Path.Combine(Keywords.History, StatesKeyword), WatcherFilter.Text) { }

    /// <summary>
    /// 找到包含指定 provinceID 的 State 文件路径, 如果没有找到则返回空字符串
    /// </summary>
    /// <param name="provinceId"></param>
    /// <returns>State 文件路径</returns>
    public string FindStatesFileByProvince(int provinceId)
    {
        foreach (var item in Resources)
        {
            if (item.Value.AsSpan().Contains(provinceId))
            {
                return item.Key;
            }
        }

        return string.Empty;
    }

    public bool TryGetProvionces(string filePath, [NotNullWhen(true)] out int[]? provinces)
    {
        if (Resources.TryGetValue(filePath, out provinces))
        {
            return true;
        }

        provinces = null;
        return false;
    }

    protected override int[]? ParseFileToContent(Node rootNode)
    {
        if (!rootNode.TryGetNode(StatesKeyword, out var statesNode))
        {
            return [];
        }

        if (!statesNode.TryGetNode("provinces", out var provincesNode))
        {
            return [];
        }

        var provinces = new List<int>(provincesNode.AllArray.Length);
        foreach (var child in provincesNode.AllArray)
        {
            if (
                !child.TryGetLeafValue(out var provinceIdLeafValue)
                || !provinceIdLeafValue.Value.TryGetInt(out int provinceId)
            )
            {
                continue;
            }

            provinces.Add(provinceId);
        }

        return provinces.ToArray();
    }
}
