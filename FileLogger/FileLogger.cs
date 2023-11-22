using System;
using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public class FileLogger : ILogger
	{
		private readonly string categoryName;
		private readonly IFileLoggerSink sink;
		private readonly Func<FileLoggerOptions> getCurrentOptions;

		public FileLogger(string categoryName, IFileLoggerSink sink, Func<FileLoggerOptions> getCurrentOptions)
		{
			ArgumentNullException.ThrowIfNull(categoryName);
			ArgumentNullException.ThrowIfNull(sink);
			ArgumentNullException.ThrowIfNull(getCurrentOptions);

			this.categoryName = categoryName;
			this.sink = sink;
			this.getCurrentOptions = getCurrentOptions;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

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

			ArgumentNullException.ThrowIfNull(state);
			ArgumentNullException.ThrowIfNull(formatter);

			sink.Pour(logLevel, eventId, categoryName, formatter(state, exception));
		}
	}
}
