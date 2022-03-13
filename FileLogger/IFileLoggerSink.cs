using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileLogger
{
	public interface IFileLoggerSink : IAsyncDisposable
	{
		void Start(FileLoggerOptions options);
#pragma warning disable CA1716
		void Stop();
#pragma warning restore CA1716
		void Pour(string message);
		ValueTask DrainAsync();
		ValueTask DrainAsync(CancellationToken cancellationToken);
	}
}
