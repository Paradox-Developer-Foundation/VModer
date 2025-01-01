namespace VModer.Core.Services.GameResource.Base;

public abstract partial class ResourcesService<TType, TContent, TParseResult>
{
    protected enum PathType : byte
    {
        File,
        Folder
    }
}