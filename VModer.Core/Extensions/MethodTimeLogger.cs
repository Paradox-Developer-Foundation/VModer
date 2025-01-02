using System.Reflection;
using NLog;

namespace VModer.Core.Extensions;

public static class MethodTimeLogger
{
    private static readonly Logger Logger = LogManager.GetLogger("MethodTime");

    public static void Log(MethodBase methodBase, TimeSpan elapsed, string message)
    {
        Logger.Debug(
            "{Name} {Message} 耗时: {Time:F3} ms",
            methodBase.Name,
            message,
            elapsed.TotalMilliseconds
        );
    }
}
