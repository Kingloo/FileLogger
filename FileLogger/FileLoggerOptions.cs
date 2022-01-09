using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public class FileLoggerOptions
	{
		public const string DefaultTimestampFormat = "dd/MM/yyyy HH:mm:ss zzz";

		public string Path { get; set; } = string.Empty;
		public LogLevel LogLevel { get; set; } = LogLevel.Information;
		public string TimestampFormat { get; set; } = DefaultTimestampFormat;
		public bool UseUtcTimestamp { get; set; } = false;
		public int DrainIntervalMs { get; set; } = 1000;
		public int DrainCount { get; set; } = 10;

		public FileLoggerOptions() { }

		public static void Validate(FileLoggerOptions options)
		{
			if (String.IsNullOrWhiteSpace(options.Path))
			{
				throw new ArgumentNullException(nameof(options.Path), "log file path was null or whitespace");
			}

			if (!File.Exists(options.Path))
			{
				throw new FileNotFoundException("file not found", options.Path);
			}
		}
	}
}
