using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using static FileLogger.Constants;

namespace FileLogger
{
	internal static class FileLoggerHelpers
	{
		internal static string FormatMessage(LogLevel logLevel, int eventId, string categoryName, string message, FileLoggerOptions options)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan<int>(eventId, 0);
			ArgumentNullException.ThrowIfNull(categoryName);
			ArgumentNullException.ThrowIfNull(message);
			ArgumentNullException.ThrowIfNull(options);

			StringBuilder sb = new StringBuilder();

			sb.Append(LeftSquareBracket);
			sb.Append(GetTimestamp(options));
			sb.Append(RightSquareBracket);
			sb.Append(Space);

			sb.Append(GetProgramName());
			sb.Append(LeftSquareBracket);
			sb.Append(Environment.ProcessId);
			sb.Append(RightSquareBracket);
			sb.Append(Space);

			sb.Append(GetLogLevelShortName(logLevel));
			sb.Append(Colon);
			sb.Append(Space);

			sb.Append(categoryName);
			sb.Append(LeftSquareBracket);
			sb.Append(eventId);
			sb.Append(RightSquareBracket);
			sb.Append(Space);

			sb.Append(message);

			return sb.ToString();
		}

		internal static string FormatSinkMessage(LogLevel logLevel, int eventId, FileLoggerOptions options, int messagesCount, bool wasCalledFromDispose)
		{
			StringBuilder categoryNameBuilder = new StringBuilder();

			categoryNameBuilder.Append(AppDomain.CurrentDomain.FriendlyName);
			categoryNameBuilder.Append(Dot);
			categoryNameBuilder.Append(FileLoggerSinkCategoryName);

			StringBuilder messageBuilder = new StringBuilder();

			messageBuilder.Append(wasCalledFromDispose ? "dispose" : "timer");
			messageBuilder.Append(Space);
			messageBuilder.Append(CultureInfo.CurrentCulture, $"drained {messagesCount} {(messagesCount == 1 ? "message" : "messages")}");

			return FormatMessage(logLevel, eventId, categoryNameBuilder.ToString(), messageBuilder.ToString(), options);
		}

		private static string GetTimestamp(FileLoggerOptions options)
		{
			DateTimeOffset time = options.UseUtcTimestamp
				? DateTimeOffset.UtcNow
				: DateTimeOffset.Now;

			return time.ToString(options.TimestampFormat, CultureInfo.CurrentCulture);
		}

		private static string GetProgramName()
		{
			return System.Diagnostics.Process.GetCurrentProcess().MainModule?.ModuleName ?? "unknown-program"; 
		}

		private static string GetProgramName(Assembly? assembly)
		{
			ArgumentNullException.ThrowIfNull(assembly);

			if (String.IsNullOrWhiteSpace(assembly.FullName))
			{
				return "unknown-program";
			}

			int commaIndex = assembly.FullName.IndexOf(',', StringComparison.Ordinal);

			return commaIndex < 0
				? assembly.FullName
				: assembly.FullName[..commaIndex];
		}

		private static string GetLogLevelShortName(LogLevel logLevel)
		{
			return logLevel switch
			{
				LogLevel.Trace => "trce",
				LogLevel.Debug => "dbug",
				LogLevel.Information => "info",
				LogLevel.Warning => "warn",
				LogLevel.Error => "fail",
				LogLevel.Critical => "crit",
				LogLevel.None => "none",
				_ => throw new ArgumentException($"not a valid LogLevel: {logLevel.ToString()}", nameof(logLevel))
			};
		}
	}
}
