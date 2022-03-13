using System;
using System.Text;
using Microsoft.Extensions.Logging;
using static FileLogger.Constants;
using static FileLogger.FileLoggerHelpers;

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
			if (formatter is null)
			{
				throw new ArgumentNullException(nameof(formatter));
			}

			if (!IsEnabled(logLevel))
			{
				return;
			}

			StringBuilder sb = new StringBuilder();

			sb.Append(categoryName);

			sb.Append(FormatEventId(eventId));
			sb.Append(Space);

			sb.Append(GetShortName(logLevel));
			sb.Append(Colon);
			sb.Append(Space);

			sb.Append(formatter(state, exception));

			sink.Pour(sb.ToString());
		}
	}
}
