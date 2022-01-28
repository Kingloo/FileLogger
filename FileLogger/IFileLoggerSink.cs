using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileLogger
{
	public interface IFileLoggerSink : IAsyncDisposable
	{
		void Start(FileLoggerOptions options);
		void Stop();
		void Pour(string message);
		ValueTask DrainAsync();
		ValueTask DrainAsync(CancellationToken cancellationToken);
	}
}
