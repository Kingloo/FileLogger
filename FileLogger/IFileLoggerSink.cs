using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public interface IFileLoggerSink : IAsyncDisposable
	{
		void StartSink(FileLoggerOptions options);
		void StopSink();
		void Pour(LogLevel logLevel, EventId eventId, string categoryName, string message);
		ValueTask DrainAsync();
		ValueTask DrainAsync(CancellationToken cancellationToken);
	}
}
