using NLog;
using ParadoxPower.CSharp;

namespace VModer.Core.Extensions;

public static class LoggerExtensions
{
	public static void LogParseError(this Logger logger, ParserError error)
	{
		logger.Warn("文件解析失败, 原因: {Message}, path: {Path}", error.ErrorMessage, error.Filename);
	}
}