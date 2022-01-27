using System;
using System.Text;
using Microsoft.Extensions.Logging;
using static FileLogger.Constants;

namespace FileLogger
{
	public class FileLogger : ILogger
	{
		private readonly string categoryName;
		private readonly IFileLoggerSink sink;
		private readonly Func<FileLoggerOptions> getCurrentOptions;

		public FileLogger(string categoryName, IFileLoggerSink sink, Func<FileLoggerOptions> getCurrentOptions)
		{
			this.categoryName = categoryName;
			this.sink = sink;
			this.getCurrentOptions = getCurrentOptions;
		}

		public IDisposable BeginScope<TState>(TState state) => default!;

		public bool IsEnabled(LogLevel logLevel)
		{
			return getCurrentOptions().MinimumLogLevel <= logLevel;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			// we don't want the options used to change during a given log call
			FileLoggerOptions currentOptions = getCurrentOptions();

			StringBuilder sb = new StringBuilder();

			DateTimeOffset time = currentOptions.UseUtcTimestamp
				? DateTimeOffset.UtcNow
				: DateTimeOffset.Now;

			sb.Append(LeftSquareBracket);
			sb.Append(time.ToString(currentOptions.TimestampFormat));
			sb.Append(RightSquareBracket);

			sb.Append(Space);

			sb.Append(GetShortName(logLevel));
			sb.Append(Colon);

			sb.Append(Space);
			sb.Append(categoryName);

			sb.Append(LeftSquareBracket);
			sb.Append(eventId);
			sb.Append(RightSquareBracket);

			sb.Append(Space);
			sb.Append(formatter(state, exception));

			sink.Pour(sb.ToString());
		}

		private static string GetShortName(LogLevel logLevel)
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
