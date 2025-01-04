using System.Diagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using MethodTimer;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure;
using VModer.Core.Services.GameResource;

namespace VModer.Core.Analyzers;

public sealed class StateAnalyzerService
{
    private readonly BuildingsService _buildingService;
    private readonly GlobalValue _provinces = new("province");
    private readonly GlobalValue _idSet = new("id");

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StateAnalyzerService(BuildingsService buildingService)
    {
        _buildingService = buildingService;
    }

    [Time]
    public List<Diagnostic> Analyze(Node node, string filePath)
    {
        var list = new List<Diagnostic>();
        if (!node.TryGetNode("state", out var stateNode))
        {
            return list;
        }

        //TODO: 当修改时怎么办? 删除文件时怎么处理
        AnalyzeId(filePath, stateNode, list);

        AnalyzeProvinces(filePath, stateNode, list);

        if (!stateNode.TryGetNode("history", out var historyNode))
        {
            return list;
        }

        AnalyzeBuildings(historyNode, list);

        return list;
    }

    private void AnalyzeBuildings(Node historyNode, List<Diagnostic> list)
    {
        Debug.Assert(historyNode.Key.Equals("history", StringComparison.OrdinalIgnoreCase));

        if (!historyNode.TryGetNode("buildings", out var buildingsNode))
        {
            return;
        }

        foreach (var buildingLeaf in buildingsNode.Leaves)
        {
            if (!buildingLeaf.Value.IsInt)
            {
                Log.Debug("num: {}", buildingLeaf.Key);
                continue;
            }

            string buildingName = buildingLeaf.Key;
            if (!_buildingService.TryGetBuildingInfo(buildingName, out var buildingInfo))
            {
                continue;
            }

            if (buildingInfo.MaxLevel is null)
            {
                continue;
            }

            if (!int.TryParse(buildingLeaf.ValueText, out int currentBuildingLevel))
            {
                continue;
            }

            if (currentBuildingLevel > buildingInfo.MaxLevel.Value)
            {
                list.Add(
                    new Diagnostic
                    {
                        Code = ErrorCode.VM1002,
                        Range = buildingLeaf.Position.ToDocumentRange(),
                        Message = $"建筑 {buildingLeaf.Key} 等级超过上限, 最大值为: {buildingInfo.MaxLevel.Value}",
                        Severity = DiagnosticSeverity.Error,
                    }
                );
            }
        }
    }
    //
    // private void AnalyzeId(string filePath, Node stateNode, List<Diagnostic> list)
    // {
    //     if (!_idSet.TryAdd(stateNode, filePath, out var info))
    //     {
    //         list.Add(
    //             new Diagnostic
    //             {
    //                 Range = info.ValuePosition,
    //                 Severity = DiagnosticSeverity.Error,
    //                 Message = $"id 重复: {info.Value}",
    //                 Code = ErrorCode.VM1001
    //             }
    //         );
    //     }
    // }
    //
    // private void AnalyzeProvinces(string filePath, Node stateNode, List<Diagnostic> list)
    // {
    //     if (!stateNode.TryGetNode("provinces", out var provincesNode))
    //     {
    //         return;
    //     }
    //
    //     foreach (var provinceLeafValue in provincesNode.LeafValues)
    //     {
    //         if (!_provinces.TryAdd(provinceLeafValue, filePath, out var info))
    //         {
    //             list.Add(
    //                 new Diagnostic
    //                 {
    //                     Range = info.ValuePosition,
    //                     Severity = DiagnosticSeverity.Error,
    //                     Message = $"province 重复: {info.Value}",
    //                     Code = ErrorCode.VM1003
    //                 }
    //             );
    //             list.Add(new Diagnostic
    //             {
    //                 
    //             });
    //         }
    //     }
    // }
}
