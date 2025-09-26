using System.Diagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.Utilities;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Services.GameResource;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Analyzers;

public sealed class StateAnalyzerService
{
    private readonly BuildingsService _buildingService;

    // private readonly GlobalValue _provinces = new("province");
    // private readonly GlobalValue _idSet = new("id");

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StateAnalyzerService(BuildingsService buildingService)
    {
        _buildingService = buildingService;
    }

    public List<Diagnostic> Analyze(Node node, string filePath)
    {
        var list = new List<Diagnostic>();
        if (!node.TryGetNode("state", out var stateNode))
        {
            return list;
        }

        //TODO: 当修改时怎么办? 删除文件时怎么处理
        // AnalyzeId(filePath, stateNode, list);

        // AnalyzeProvinces(filePath, stateNode, list);

        if (!stateNode.TryGetNode("history", out var historyNode))
        {
            return list;
        }

        AnalyzeVictoryPoints(stateNode, historyNode, list);
        AnalyzeBuildings(historyNode, list);

        return list;
    }

    private void AnalyzeVictoryPoints(Node stateNode, Node historyNode, List<Diagnostic> list)
    {
        var victoryPoints = new List<(VictoryPoint, Position.Range)>();
        foreach (
            var item in historyNode.Nodes.Where(node =>
                node.Key.Equals("victory_points", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            var values = item.LeafValues.ToArray();
            if (values.Length != 2)
            {
                continue;
            }

            var provinceIdLeafValue = values[0];
            var valueLeafValue = values[1];
            if (
                !provinceIdLeafValue.Value.TryGetInt(out var provinceId)
                || !valueLeafValue.Value.TryGetInt(out var value)
            )
            {
                continue;
            }

            victoryPoints.Add((new VictoryPoint(provinceId, value), item.Position));
        }

        if (victoryPoints.Count == 0)
        {
            return;
        }

        if (!stateNode.TryGetNode("provinces", out var provincesNode))
        {
            return;
        }

        var provinceIds = provincesNode
            .LeafValues.AsValueEnumerable()
            .Where(leafValue => leafValue.Value.IsInt)
            .Select(leafValue =>
            {
                leafValue.Value.TryGetInt(out var id);
                return id;
            })
            .ToHashSet();

        foreach (var item in victoryPoints)
        {
            var victoryPoint = item.Item1;
            if (!provinceIds.Contains(victoryPoint.ProvinceId))
            {
                list.Add(
                    new Diagnostic
                    {
                        Code = ErrorCode.VM2001,
                        Range = item.Item2.ToDocumentRange(),
                        Message = string.Format(
                            Resources.ErrorMessage_VictoryPointProvinceNotInState,
                            victoryPoint.ProvinceId
                        ),
                        Severity = DiagnosticSeverity.Hint
                    }
                );
            }
        }
    }

    private void AnalyzeBuildings(Node historyNode, List<Diagnostic> list)
    {
        Debug.Assert(historyNode.Key.Equals("history", StringComparison.OrdinalIgnoreCase));

        if (!historyNode.TryGetNode("buildings", out var buildingsNode))
        {
            return;
        }

        foreach (var child in buildingsNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                AnalyzeBuildingLeaf(leaf, list);
            }
            // 是省份建筑
            else if (child.TryGetNode(out var provinceBuildingsNode))
            {
                foreach (var buildingLeaf in provinceBuildingsNode.Leaves)
                {
                    AnalyzeBuildingLeaf(buildingLeaf, list);
                }
            }
        }
    }

    private void AnalyzeBuildingLeaf(Leaf buildingLeaf, List<Diagnostic> list)
    {
        if (!buildingLeaf.Value.IsInt)
        {
            return;
        }

        string buildingName = buildingLeaf.Key;
        if (!_buildingService.TryGetBuildingInfo(buildingName, out var buildingInfo))
        {
            return;
        }

        if (buildingInfo.MaxLevel is null)
        {
            return;
        }

        if (!long.TryParse(buildingLeaf.ValueText, out long currentBuildingLevel))
        {
            return;
        }

        if (currentBuildingLevel > buildingInfo.MaxLevel.Value)
        {
            list.Add(
                new Diagnostic
                {
                    Code = ErrorCode.VM1002,
                    Range = buildingLeaf.Position.ToDocumentRange(),
                    Message = string.Format(
                        Resources.ErrorMessage_BuildingLevelExceedsMaxValue,
                        buildingLeaf.Key,
                        buildingInfo.MaxLevel.Value
                    ),
                    Severity = DiagnosticSeverity.Error,
                }
            );
        }
    }

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
