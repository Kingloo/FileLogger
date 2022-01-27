using System;
using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

			optionsChangeToken = monitoredFileLoggerOptions.OnChange(updatedOptions => options = updatedOptions);

			this.sink = sink;
		}

		public FileLoggerOptions GetCurrentOptions() => options;

		public ILogger CreateLogger(string categoryName)
		{
			return loggers.GetOrAdd(categoryName, name => new FileLogger(name, sink, GetCurrentOptions));
		}

		public void Dispose()
		{
			loggers.Clear();
			optionsChangeToken.Dispose();
		}
	}
}
