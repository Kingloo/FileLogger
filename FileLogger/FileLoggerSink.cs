using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FileLogger
{
	public class FileLoggerSink : IFileLoggerSink
	{
		private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
		private Timer? queueTimer;

		private FileLoggerOptions _options = new FileLoggerOptions();
		public FileLoggerOptions Options { get => _options; }

		public FileLoggerSink() { }

		public void Start(FileLoggerOptions options)
		{
			_options = options;

			StartDrainTimer();
		}

		private void StartDrainTimer()
		{
			if (queueTimer is null)
			{
				queueTimer = new Timer(_options.DrainInterval.TotalMilliseconds);
				queueTimer.Elapsed += QueueTimer_Elapsed;
				queueTimer.Start();
			}
		}

		private async void QueueTimer_Elapsed(object? sender, ElapsedEventArgs e)
		{
			await DrainQueueAndWriteAsync().ConfigureAwait(false);
		}

		public void Pour(string message)
		{
			message = String.IsNullOrWhiteSpace(message) ? "empty message" : message;

			queue.Enqueue(message);
		}

		public void Stop()
		{
			StopDrainTimer();
		}

		public ValueTask DrainAsync() => DrainQueueAndWriteAsync();

		private void StopDrainTimer()
		{
			if (queueTimer is not null)
			{
				queueTimer.Stop();
				queueTimer.Elapsed -= QueueTimer_Elapsed;
				queueTimer.Dispose();
			}
		}

		private (string, int) DrainQueue(bool drainEntireQueue = false)
		{
			if (queue.Count == 0)
			{
				return (string.Empty, 0);
			}

			StringBuilder sb = new StringBuilder();

			int drainedCount = 0;

			int maxMessagesToDrainThisRun = drainEntireQueue
				? Int32.MaxValue
				: _options.DrainCount;

			while (drainedCount < maxMessagesToDrainThisRun
				&& queue.TryDequeue(out string? log))
			{
				if (!String.IsNullOrWhiteSpace(log))
				{
					sb.AppendLine(log);
				}

				drainedCount++;
			}

			return (sb.ToString(), drainedCount);
		}

		private ValueTask DrainQueueAndWriteAsync()
			=> DrainQueueAndWriteAsync(fromDispose: false);

		private async ValueTask DrainQueueAndWriteAsync(bool fromDispose)
		{
			(string log, int count) = DrainQueue();

			string drainMessage = GetDrainMessage(count, fromDispose: fromDispose);

			if (count > 0)
			{
				log = String.IsNullOrWhiteSpace(log) ? "empty message" : log;

				await WriteToFileAsync(log).ConfigureAwait(false);
			}

			await WriteLineToFileAsync(drainMessage).ConfigureAwait(false);

			if (fromDispose)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100d)).ConfigureAwait(false);
			}
		}

		private string GetDrainMessage(int count, bool fromDispose)
		{
			DateTimeOffset time = Options.UseUtcTimestamp
				? DateTimeOffset.UtcNow
				: DateTimeOffset.Now;

			string timestamp = Options.UseUtcTimestamp
				? $"[{time.ToString(_options.TimestampFormat)}]"
				: $"[{time.ToString(_options.TimestampFormat)}]";

			string processName = $"{Process.GetCurrentProcess().ProcessName}.Sink";
			string drainCountMessage = $"drained {count} {(count == 1 ? "message" : "messages")}";

			return fromDispose
				? $"{timestamp} sink: {processName} disposed {drainCountMessage}"
				: $"{timestamp} sink: {processName} timer {drainCountMessage} (tick {(queueTimer?.Interval.ToString() ?? "unknown")} ms)";
		}

		private async ValueTask WriteToFileAsync(string message)
		{
			if (!File.Exists(_options.Path))
			{
				await Console.Error.WriteLineAsync($"file not found: {Options.Path}").ConfigureAwait(false);

				return;
			}

			FileStream? fsAsync = null;

			try
			{
				fsAsync = new FileStream(
					_options.Path,
					FileMode.Append,
					FileAccess.Write,
					FileShare.ReadWrite,
					4096,
					FileOptions.Asynchronous);

				await fsAsync.WriteAsync(Encoding.UTF8.GetBytes(message)).ConfigureAwait(false);

				await fsAsync.FlushAsync().ConfigureAwait(false);
			}
			finally
			{
				if (fsAsync is not null)
				{
					await fsAsync.DisposeAsync().ConfigureAwait(false);
				}
			}
		}

		private ValueTask WriteLineToFileAsync(string message)
		{
			return WriteToFileAsync(AppendNewLine(message));
		}

		private static string AppendNewLine(string message)
		{
			return $"{message}{Environment.NewLine}";
		}

		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			StopDrainTimer();

			await DrainQueueAndWriteAsync(fromDispose: true).ConfigureAwait(false);
		}
	}
}
