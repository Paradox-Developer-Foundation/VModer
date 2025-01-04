using EmmyLua.LanguageServer.Framework.Protocol.Model;

namespace VModer.Core.Infrastructure;

/// <summary>
/// 重复存在的值的信息
/// </summary>
/// <param name="FilePath">重复存在的值的文件路径</param>
/// <param name="ValuePosition">值的位置信息</param>
/// <param name="Value">值</param>
public sealed record RepetitiveValueInfo(string FilePath, DocumentRange ValuePosition, int Value);
