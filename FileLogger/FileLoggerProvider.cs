using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileLogger
{
	public sealed class FileLoggerProvider : ILoggerProvider
	{
		private readonly IOptions<FileLoggerOptions> options;
		private readonly IFileLoggerSink sink;

		private readonly ConcurrentDictionary<string, FileLogger> loggers = new ConcurrentDictionary<string, FileLogger>();

		public FileLoggerProvider(IOptions<FileLoggerOptions> options, IFileLoggerSink sink)
		{
			this.options = options;
			this.sink = sink;
		}

		public ILogger CreateLogger(string categoryName)
		{
			return loggers.GetOrAdd(categoryName, name => new FileLogger(name, options, sink));
		}

		public void Dispose()
		{
			loggers.Clear();
		}
	}
}
