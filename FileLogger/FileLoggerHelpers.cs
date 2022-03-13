using System;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using static FileLogger.Constants;

namespace FileLogger
{
	internal static class FileLoggerHelpers
	{
		internal static string CreateFileLoggerSinkCategoryNameAndEventId(EventId eventId)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(AppDomain.CurrentDomain.FriendlyName);
			sb.Append(Dot);
			sb.Append(FileLoggerSinkCategoryName);

			sb.Append(FormatEventId(eventId));

			return sb.ToString();
		}

		internal static string PrependTimestamp(string message, DateTimeOffset time, FileLoggerOptions options)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(LeftSquareBracket);
			sb.Append(time.ToString(options.TimestampFormat, CultureInfo.CurrentCulture));
			sb.Append(RightSquareBracket);

			sb.Append(Space);

			sb.Append(message);

			return sb.ToString();
		}

		internal static string FormatEventId(EventId eventId)
		{
			return FormatEventId(eventId.Id);
		}

		internal static string FormatEventId(int eventId)
		{
			return $"[{eventId}]";
		}

		internal static string GetShortName(LogLevel logLevel)
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
