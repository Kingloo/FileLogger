using System;
using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public interface IFileLogSink : IAsyncDisposable
	{
		FileLoggerOptions Options { get; }

		void Start(FileLoggerOptions options);
		void Pour(LogLevel logLevel, string message);
		void Stop();
	}
}
