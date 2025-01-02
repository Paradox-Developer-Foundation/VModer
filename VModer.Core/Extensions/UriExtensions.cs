namespace VModer.Core.Extensions;

public static class UriExtensions
{
    public static string ToSystemPath(this Uri uri)
    {
        string fileSystemPath = Uri.UnescapeDataString(uri.AbsolutePath);
        if (OperatingSystem.IsWindows())
        {
            if (fileSystemPath.StartsWith('/'))
            {
                fileSystemPath = fileSystemPath.TrimStart('/');
            }
            fileSystemPath = fileSystemPath.Replace('/', '\\');
        }
        return fileSystemPath;
    }
}