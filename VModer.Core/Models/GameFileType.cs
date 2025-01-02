using Ardalis.SmartEnum;
using DotNet.Globbing;
using MethodTimer;

namespace VModer.Core.Models;

public sealed class GameFileType(string name, string value) : SmartEnum<GameFileType, string>(name, value)
{
    public static readonly GameFileType Unknown = new(nameof(Unknown), string.Empty);
    public static readonly GameFileType State = new(nameof(State), "**/history/states/*.txt");

    private static readonly Dictionary<string, Glob> Globs = new();

    static GameFileType()
    {
        GlobOptions.Default.Evaluation.CaseInsensitive = true;
    }

    [Time]
    public static GameFileType FromFilePath(string filePath)
    {
        foreach (var fileType in List.Where(type => type != Unknown))
        {
            if (IsGlobMatch(fileType, filePath))
            {
                return fileType;
            }
        }

        return Unknown;
    }

    private static bool IsGlobMatch(GameFileType fileType, string filePath)
    {
        string? key = fileType.Name;
        string? patter = fileType.Value;
        if (!Globs.TryGetValue(key, out var glob))
        {
            glob = Glob.Parse(patter);
            Globs.Add(key, glob);
        }

        return glob.IsMatch(filePath);
    }
}
