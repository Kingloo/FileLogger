using System;
using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public interface IFileLogSink : IAsyncDisposable
	{
		FileLoggerOptions Options { get; }

		void Start(FileLoggerOptions options);
		void Pour(string message);
		void Stop();
	}
}
