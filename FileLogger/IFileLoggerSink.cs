using System;
using System.Threading.Tasks;

namespace FileLogger
{
	public interface IFileLoggerSink : IAsyncDisposable
	{
		FileLoggerOptions Options { get; }

		void Start(FileLoggerOptions options);
		void Pour(string message);
		ValueTask DrainAsync();
		void Stop();
	}
}
