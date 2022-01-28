using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using static FileLogger.Constants;
using static FileLogger.FileLoggerHelpers;

namespace FileLogger
{
	public class FileLoggerSink : IFileLoggerSink
	{
		private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
		private System.Timers.Timer? queueTimer;

		private FileLoggerOptions options = new FileLoggerOptions();

		public FileLoggerSink() { }

		public void Start(FileLoggerOptions options)
		{
			this.options = options;

			StartDrainTimer();
		}

		private void StartDrainTimer()
		{
			if (queueTimer is null)
			{
				queueTimer = new System.Timers.Timer(options.DrainIntervalMs);
				queueTimer.Elapsed += QueueTimer_Elapsed;
				queueTimer.Start();
			}
		}

		public void Stop()
		{
			StopDrainTimer();
		}

		private void StopDrainTimer()
		{
			if (queueTimer is not null)
			{
				queueTimer.Stop();
				queueTimer.Elapsed -= QueueTimer_Elapsed;
				queueTimer.Dispose();
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

		public ValueTask DrainAsync()
			=> DrainAsync(CancellationToken.None);

		public ValueTask DrainAsync(CancellationToken cancellationToken)
			=> DrainQueueAndWriteAsync(fromDispose: false, cancellationToken);

		private ValueTask DrainQueueAndWriteAsync()
			=> DrainQueueAndWriteAsync(fromDispose: false, CancellationToken.None);

		private ValueTask DrainQueueAndWriteAsync(bool fromDispose)
			=> DrainQueueAndWriteAsync(fromDispose, CancellationToken.None);

		private ValueTask DrainQueueAndWriteAsync(CancellationToken cancellationToken)
			=> DrainQueueAndWriteAsync(fromDispose: false, cancellationToken);

		private async ValueTask DrainQueueAndWriteAsync(bool fromDispose, CancellationToken cancellationToken)
		{
			IList<string> messages = DrainQueue(drainEntireQueue: fromDispose);

			if (fromDispose == false
				&& messages.Count == 0
				&& options.MinimumLogLevel > LogLevel.Trace)
			{
				// to avoid log spam saying that timer ran but drained no new messages
				// we don't run if
				//		not from dispose
				//		there are no message
				//		FileLoggerOptions is above Trace

				return;
			}

			int eventId = fromDispose ? LogEventIds.Disposed : LogEventIds.Timer;

			string drainMessage = CreateDrainMessage(messages, eventId, fromDispose: fromDispose);

			await WriteToFileAsync(drainMessage).ConfigureAwait(false);

			if (fromDispose)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100d)).ConfigureAwait(false);
			}
		}

		private IList<string> DrainQueue()
			=> DrainQueue(drainEntireQueue: false);

		private IList<string> DrainQueue(bool drainEntireQueue)
		{
			if (queue.Count == 0)
			{
				return Array.Empty<string>();
			}

			int drainedCount = 0;

			List<string> messages = new List<string>();

			int maxMessagesToDrainThisRun = drainEntireQueue
				? Int32.MaxValue
				: options.DrainCount;

			while (drainedCount < maxMessagesToDrainThisRun
				&& queue.TryDequeue(out string? message))
			{
				messages.Add(message);

				drainedCount++;
			}

			return messages;
		}

		private string CreateDrainMessage(IList<string> messages, int eventId, bool fromDispose)
		{
			StringBuilder sb = new StringBuilder();

			DateTimeOffset time = options.UseUtcTimestamp
				? DateTimeOffset.UtcNow
				: DateTimeOffset.Now;

			foreach (string message in messages)
			{
				string messagePrependedWithTimestamp = PrependTimestamp(message, time, options);

				sb.AppendLine(messagePrependedWithTimestamp);
			}

			string sinkMessage = CreateSinkMessage(messages.Count, eventId, fromDispose);
			string sinkMessagePrependedWithTimestamp = PrependTimestamp(sinkMessage, time, options);

			sb.AppendLine(sinkMessagePrependedWithTimestamp);

			return sb.ToString();
		}

		private static string CreateSinkMessage(int messagesCount, int eventId, bool fromDispose)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(CreateFileLoggerSinkCategoryNameAndEventId(eventId));

			if (fromDispose)
			{
				sb.Append(" Dispose ");
			}
			else
			{
				sb.Append(" Timer ");
			}

			sb.Append($"drained {messagesCount} {(messagesCount == 1 ? "message" : "messages")}");

			return sb.ToString();
		}

		private async ValueTask WriteToFileAsync(string message)
		{
			FileStream? fsAsync = null;

			try
			{
				fsAsync = new FileStream(
					options.Path,
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

		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			StopDrainTimer();

			await DrainQueueAndWriteAsync(fromDispose: true).ConfigureAwait(false);
		}
	}
}
