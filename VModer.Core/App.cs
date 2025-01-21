namespace VModer.Core;

public static class App
{
    public static string AssetsFolder { get; } = Path.Combine(AppContext.BaseDirectory, "Assets");
    public static IServiceProvider Services { get; set; } = null!;
}