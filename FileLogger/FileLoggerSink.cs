using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using static FileLogger.FileLoggerHelpers;

namespace FileLogger
{
	public sealed class FileLoggerSink : IFileLoggerSink
	{
		private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
		private System.Timers.Timer? queueTimer;

		private FileLoggerOptions options = new FileLoggerOptions();

		public FileLoggerSink() { }

		public FileLoggerSink(FileLoggerOptions options)
		{
			if (options is null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			this.options = options;
		}

		public void StartSink(FileLoggerOptions options)
		{
			if (options is null)
			{
				throw new ArgumentNullException(nameof(options));
			}

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

		public void StopSink()
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
				queueTimer = null;
			}
		}

		private async void QueueTimer_Elapsed(object? sender, ElapsedEventArgs e)
		{
			await DrainQueueAndWriteAsync(wasCalledFromDispose: false, CancellationToken.None).ConfigureAwait(false);
		}

		public void Pour(LogLevel logLevel, EventId eventId, string categoryName, string message)
		{
			if (String.IsNullOrWhiteSpace(categoryName))
			{
				throw new ArgumentNullException(nameof(categoryName), $"your {nameof(categoryName)} was null-or-whitespace");
			}

			if (String.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message), $"your {nameof(message)} was null-or-whitespace");
			}

			string formattedMessage = FormatMessage(logLevel, eventId.Id, categoryName, message, options);

			queue.Enqueue(formattedMessage);
		}

		public ValueTask DrainAsync()
			=> DrainAsync(CancellationToken.None);

		public ValueTask DrainAsync(CancellationToken cancellationToken)
			=> DrainQueueAndWriteAsync(wasCalledFromDispose: false, cancellationToken);

		private async ValueTask DrainQueueAndWriteAsync(bool wasCalledFromDispose, CancellationToken cancellationToken)
		{
			IList<string> messages = DrainQueue(drainEntireQueue: wasCalledFromDispose);

			if (wasCalledFromDispose == false
				&& messages.Count == 0)
			{
				return;
			}

			int eventId = wasCalledFromDispose
				? EventIds.Disposed
				: EventIds.Timer;

			string drainMessage = CreateDrainMessage(messages, eventId, wasCalledFromDispose: wasCalledFromDispose);

			await WriteToFileAsync(drainMessage, options, cancellationToken).ConfigureAwait(false);

			if (wasCalledFromDispose)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100d), cancellationToken).ConfigureAwait(false);
			}
		}

		private IList<string> DrainQueue(bool drainEntireQueue)
		{
			if (queue.IsEmpty)
			{
				return Array.Empty<string>();
			}

			uint drainedCount = 0;

			IList<string> messages = new List<string>();

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

		private string CreateDrainMessage(IList<string> messages, int eventId, bool wasCalledFromDispose)
		{
			StringBuilder sb = new StringBuilder();

			foreach (string message in messages)
			{
				sb.AppendLine(message);
			}

			if (options.IncludeProviderMessages)
			{
				string sinkLogMessage = FormatSinkMessage(LogLevel.Information, eventId, options, messages.Count, wasCalledFromDispose);

				sb.AppendLine(sinkLogMessage);
			}

			return sb.ToString();
		}

		private static async ValueTask WriteToFileAsync(string message, FileLoggerOptions options, CancellationToken cancellationToken)
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

				await fsAsync.WriteAsync(Encoding.UTF8.GetBytes(message), cancellationToken).ConfigureAwait(false);

				await fsAsync.FlushAsync(CancellationToken.None).ConfigureAwait(false);
			}
			finally
			{
// analysis mode->all asserts that the following 'is not null' check will always be true
// but without that check you get yellow squigglies under first fsAsync warning that it might be null
# pragma warning disable CA1508
				if (fsAsync is not null)
				{
					await fsAsync.DisposeAsync().ConfigureAwait(false);
				}
# pragma warning restore CA1508
			}
		}

		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			StopDrainTimer();

			await DrainQueueAndWriteAsync(wasCalledFromDispose: true, CancellationToken.None).ConfigureAwait(false);
		}
	}
}
