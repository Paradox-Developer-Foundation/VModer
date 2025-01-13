namespace VModer.Core;

public static class ErrorCode
{
    /// <summary>
    /// 解析错误
    /// </summary>
    public const string VM1000 = nameof(VM1000);
    /// <summary>
    /// state id 重复
    /// </summary>
    public const string VM1001 = nameof(VM1001);
    /// <summary>
    /// 建筑等级超过上限
    /// </summary>
    public const string VM1002 = nameof(VM1002);
    /// <summary>
    /// province 重复使用
    /// </summary>
    public const string VM1003 = nameof(VM1003);
    /// <summary>
    /// 人物技能等级超过上限
    /// </summary>
    public const string VM1004 = nameof(VM1004);
}