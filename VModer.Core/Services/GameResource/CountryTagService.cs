using System.Collections.Frozen;
using ParadoxPower.Process;
using VModer.Core.Infrastructure;
using VModer.Core.Services.GameResource.Base;

namespace VModer.Core.Services.GameResource;

public sealed class CountryTagService : CommonResourcesService<CountryTagService, FrozenSet<string>>
{
    /// <summary>
    /// 在游戏内注册的国家标签
    /// </summary>
    public IReadOnlyCollection<string> CountryTags => _countryTagsLazy.Value;

    private readonly ResetLazy<string[]> _countryTagsLazy;

    public CountryTagService()
        : base(Path.Combine(Keywords.Common, "country_tags"), WatcherFilter.Text)
    {
        _countryTagsLazy = new ResetLazy<string[]>(
            () => Resources.Values.SelectMany(set => set.Items).ToArray()
        );
        OnResourceChanged += (_, _) =>
        {
            _countryTagsLazy.Reset();
            Log.Debug("Country tags changed, 已重置");
        };
    }

    protected override FrozenSet<string>? ParseFileToContent(Node rootNode)
    {
        var leaves = rootNode.Leaves.ToArray();
        // 不加载临时标签
        if (
            Array.Exists(
                leaves,
                leaf =>
                    leaf.Key.Equals("dynamic_tags", StringComparison.OrdinalIgnoreCase)
                    && leaf.ValueText.Equals("yes", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return null;
        }

        var countryTags = new HashSet<string>(leaves.Length);
        foreach (var leaf in leaves)
        {
            string? countryTag = leaf.Key;
            // 国家标签长度必须为 3
            if (countryTag.Length != 3)
            {
                continue;
            }
            countryTags.Add(countryTag);
        }
        return countryTags.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
