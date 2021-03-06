using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public class FileLoggerOptions
	{
		public const string DefaultTimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz";

		public string Path { get; set; } = string.Empty;
		public LogLevel MinimumLogLevel { get; set; } = LogLevel.Trace;
		public bool IncludeProviderMessages { get; set; } = false;
		public string TimestampFormat { get; set; } = DefaultTimestampFormat;
		public bool UseUtcTimestamp { get; set; } = false;
		public int DrainIntervalMs { get; set; } = 5000;
		public int DrainCount { get; set; } = 50;

		public FileLoggerOptions() { }
	}
}
