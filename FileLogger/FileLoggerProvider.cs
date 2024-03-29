using System;
using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static FileLogger.Constants;
using static FileLogger.EventIds;

namespace FileLogger
{
	[UnsupportedOSPlatform("browser")]
	[ProviderAlias("FileLogger")]
	public sealed class FileLoggerProvider : ILoggerProvider
	{
		private readonly IDisposable? optionsChangeToken;
		private FileLoggerOptions options;
		private readonly IFileLoggerSink sink;
		private readonly ConcurrentDictionary<string, FileLogger> loggers = new ConcurrentDictionary<string, FileLogger>();

// it thinks that passing monitoredOptions to the other ctor counts as a use, even if we are checking it in the other ctor
#pragma warning disable CA1062
		public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> monitoredOptions, IFileLoggerSink sink)
			: this(monitoredOptions.CurrentValue, sink)
#pragma warning restore CA1062
		{
			ArgumentNullException.ThrowIfNull(monitoredOptions);

			optionsChangeToken = monitoredOptions.OnChange(OnOptionsChanged);
		}

		public FileLoggerProvider(FileLoggerOptions options, IFileLoggerSink sink)
		{
			ArgumentNullException.ThrowIfNull(options);
			ArgumentNullException.ThrowIfNull(sink);

			this.options = options;
			this.sink = sink;
		}

		private void OnOptionsChanged(FileLoggerOptions updatedOptions)
		{
			options = updatedOptions;

			sink.Pour(LogLevel.Information, OptionsUpdated, FileLoggerSinkCategoryName, "options updated");
		}

# pragma warning disable CA1024
		public FileLoggerOptions GetCurrentOptions() => options;
# pragma warning restore CA1024

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

					optionsChangeToken?.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
