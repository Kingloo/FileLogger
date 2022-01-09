using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileLogger
{
	public class FileLogger : ILogger
	{
		private readonly string categoryName;
		private readonly FileLoggerOptions options;
		private readonly IFileLoggerSink sink;

		public FileLogger(string categoryName, IOptions<FileLoggerOptions> options, IFileLoggerSink sink)
		{
			this.categoryName = categoryName;
			this.options = options.Value;
			this.sink = sink;
		}

		public IDisposable BeginScope<TState>(TState state) => default!;

		public bool IsEnabled(LogLevel logLevel) => logLevel >= options.LogLevel;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			StringBuilder sb = new StringBuilder();

			DateTimeOffset time = options.UseUtcTimestamp
				? DateTimeOffset.UtcNow
				: DateTimeOffset.Now;

			sb.Append($"[{time.ToString(options.TimestampFormat)}]");
			sb.Append(" ");
			sb.Append($"{GetShortName(logLevel)}:");
			sb.Append(" ");
			sb.Append($"{categoryName}");
			sb.Append(" ");
			sb.Append(formatter(state, exception));

			sink.Pour(sb.ToString());
		}

		private static string GetShortName(LogLevel logLevel)
		{
			return logLevel switch
			{
				LogLevel.None => "none",
				LogLevel.Trace => "trce",
				LogLevel.Debug => "dbug",
				LogLevel.Information => "info",
				LogLevel.Warning => "warn",
				LogLevel.Error => "fail",
				LogLevel.Critical => "crit",
				_ => throw new ArgumentException($"not a valid LogLevel: {logLevel.ToString()}", nameof(logLevel))
			};
		}
	}
}
