using Ardalis.SmartEnum;

namespace VModer.Core.Services.GameResource.Base;

public abstract partial class ResourcesService<TType, TContent, TParseResult>
{
    protected sealed class WatcherFilter : SmartEnum<WatcherFilter, byte>
    {
        public static readonly WatcherFilter AllFiles = new("*.*", 0);
        public static readonly WatcherFilter Text = new("*.txt", 1);
        public static readonly WatcherFilter LocalizationFiles = new("*.yml", 2);
        public static readonly WatcherFilter InterfaceCoreGfxFile = new("core.gfx", 3);
        public static readonly WatcherFilter GfxFiles = new("*.gfx", 4);
        public static readonly WatcherFilter Lua = new("*.lua", 5);

        private WatcherFilter(string name, byte value) : base(name, value)
        {
        }
    }
}
