using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using static FileLogger.Constants;

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
				queueTimer = new Timer(_options.DrainIntervalMs);
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

			if (fromDispose == false
				&& count == 0
				&& Options.MinimumLogLevel > LogLevel.Trace)
			{
				// if from Dispose, we write out everything no matter what
				// if not from Dispose, we are from timer
				// when from timer we only write out messages if there any
				// on Trace we log timer spew messages as well

				return;
			}

			int eventId = fromDispose
				? LogEventIds.DrainDisposed
				: LogEventIds.DrainTimer;

			string drainMessage = GetDrainMessage(count, eventId, fromDispose: fromDispose);

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

		private string GetDrainMessage(int count, int eventId, bool fromDispose)
		{
			DateTimeOffset time = Options.UseUtcTimestamp
				? DateTimeOffset.UtcNow
				: DateTimeOffset.Now;

			StringBuilder sb = new StringBuilder();

			sb.Append(LeftSquareBracket);
			sb.Append(time.ToString(_options.TimestampFormat));
			sb.Append(RightSquareBracket);

			sb.Append(" sink: ");

			sb.Append(AppDomain.CurrentDomain.FriendlyName);
			sb.Append(".Sink");

			sb.Append(LeftSquareBracket);
			sb.Append(eventId);
			sb.Append(RightSquareBracket);

			if (fromDispose)
			{
				sb.Append(" disposed ");
			}
			else
			{
				sb.Append(" timer ");
			}

			sb.Append($"drained {count} {(count == 1 ? "message" : "messages")}");

			return sb.ToString();
		}

		private async ValueTask WriteToFileAsync(string message)
		{
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
			}
			finally
			{
				if (fsAsync is not null)
				{
					await fsAsync.FlushAsync().ConfigureAwait(false);

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
