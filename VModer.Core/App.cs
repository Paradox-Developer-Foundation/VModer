namespace VModer.Core;

public static class App
{
    public static string AssetsFolder => Path.Combine([Environment.CurrentDirectory, "Assets"]);
    public static IServiceProvider Services { get; set; } = null!;
}