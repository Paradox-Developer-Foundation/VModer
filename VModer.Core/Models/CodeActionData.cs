using System.Text.Json.Serialization;

namespace VModer.Core.Models;

[JsonSerializable(typeof(CodeActionData))]
internal partial class CodeActionDataContext : JsonSerializerContext;

public sealed class CodeActionData(string errorCode, Uri filePath)
{
    public string ErrorCode { get; set; } = errorCode;
    public Uri FilePath { get; set; } = filePath;
}