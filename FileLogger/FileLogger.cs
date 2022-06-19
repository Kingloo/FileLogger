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
			if (String.IsNullOrWhiteSpace(categoryName))
			{
				throw new ArgumentNullException(nameof(categoryName));
			}

			if (sink is null)
			{
				throw new ArgumentNullException(nameof(sink));
			}

			if (getCurrentOptions is null)
			{
				throw new ArgumentNullException(nameof(getCurrentOptions));
			}

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

			if (state is null)
			{
				throw new ArgumentNullException(nameof(state));
			}

			if (formatter is null)
			{
				throw new ArgumentNullException(nameof(formatter));
			}

			sink.Pour(logLevel, eventId, categoryName, formatter(state, exception));
		}
	}
}
