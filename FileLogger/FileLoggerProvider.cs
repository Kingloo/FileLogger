using System;
using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static FileLogger.FileLoggerHelpers;

namespace FileLogger
{
	[UnsupportedOSPlatform("browser")]
	[ProviderAlias("FileLogger")]
	public sealed class FileLoggerProvider : ILoggerProvider
	{
		private readonly IDisposable optionsChangeToken;
		private FileLoggerOptions options;
		private readonly IFileLoggerSink sink;
		private readonly ConcurrentDictionary<string, FileLogger> loggers = new ConcurrentDictionary<string, FileLogger>();

		public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> monitoredFileLoggerOptions, IFileLoggerSink sink)
		{
			this.options = monitoredFileLoggerOptions.CurrentValue;

			optionsChangeToken = monitoredFileLoggerOptions.OnChange(OnOptionsChanged);

			this.sink = sink;
		}

		private void OnOptionsChanged(FileLoggerOptions updatedOptions)
		{
			options = updatedOptions;

			string categoryNameAndEventId = CreateFileLoggerSinkCategoryNameAndEventId(LogEventIds.OptionsUpdated);

			sink.Pour($"{categoryNameAndEventId} options updated");
		}

		public FileLoggerOptions GetCurrentOptions() => options;

		public ILogger CreateLogger(string categoryName)
		{
			return loggers.GetOrAdd(categoryName, name => new FileLogger(name, sink, GetCurrentOptions));
		}

		private bool disposedValue = false;

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					loggers.Clear();

					optionsChangeToken.Dispose();
				}

				disposedValue = true;
			}
		}

		void IDisposable.Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
