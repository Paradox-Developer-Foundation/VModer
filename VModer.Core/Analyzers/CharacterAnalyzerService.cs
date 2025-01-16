using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Models.Character;
using VModer.Core.Services.GameResource;

namespace VModer.Core.Analyzers;

public sealed class CharacterAnalyzerService
{
    private readonly CharacterSkillService _characterSkillService;

    public CharacterAnalyzerService(CharacterSkillService characterSkillService)
    {
        _characterSkillService = characterSkillService;
    }

    public List<Diagnostic> Analyze(Node rootNode)
    {
        var list = new List<Diagnostic>();
        foreach (
            var charactersNode in rootNode.Nodes.Where(node =>
                node.Key.Equals("characters", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            foreach (var character in charactersNode.Nodes)
            {
                foreach (var childNode in character.Nodes)
                {
                    if (
                        !Array.Exists(
                            Keywords.GeneralKeywords,
                            keyword => childNode.Key.Equals(keyword, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        continue;
                    }

                    AnalyzeCharacter(childNode, list);
                }
            }
        }

        return list;
    }

    private void AnalyzeCharacter(Node generalNode, List<Diagnostic> list)
    {
        var skillType = SkillCharacterType.FromCharacterType(generalNode.Key);
        foreach (var skillLeaf in generalNode.Leaves)
        {
            if (!skillLeaf.Value.TryGetInt(out int value))
            {
                continue;
            }

            var skill = SkillType.List.FirstOrDefault(skill =>
                skill.Value.Equals(skillLeaf.Key, StringComparison.OrdinalIgnoreCase)
            );
            if (skill is null)
            {
                continue;
            }

            ushort maxValue = _characterSkillService.GetMaxSkillValue(skill, skillType);
            if (value > maxValue)
            {
                list.Add(
                    new Diagnostic
                    {
                        Range = skillLeaf.Position.ToDocumentRange(),
                        Message = $"{generalNode.Key} 的属性 {skillLeaf.Key} 超过最大值 {maxValue}",
                        Severity = DiagnosticSeverity.Error,
                        Code = ErrorCode.VM1004
                    }
                );
            }
        }
    }
}
