using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace FileLogger
{
	public class Program
	{
		public static async Task<int> Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Error.WriteLine("[FileLogger] no file argument");

				return -1;
			}

			string path = args[0];

			if (!File.Exists(path))
			{
				Console.Error.WriteLine($"[FileLogger] file not found: {(String.IsNullOrWhiteSpace(path) ? "empty path" : path)}");

				return -1;
			}

			await using IFileLogSink sink = new FileLogSink();

			IServiceProvider serviceProvider = new ServiceCollection()
				.AddSingleton(sink)
				.AddLogging(logging =>
				{
					logging.SetMinimumLevel(LogLevel.Trace);

					logging.AddSimpleConsole(simpleConsoleOptions =>
					{
						simpleConsoleOptions.ColorBehavior = LoggerColorBehavior.Enabled;
						simpleConsoleOptions.TimestampFormat = $"[{FileLoggerOptions.DefaultTimestampFormat}] ";
						simpleConsoleOptions.SingleLine = true;
					});

					logging.AddFileLogger(options =>
					{
						options.Path = @".\test.txt";
					});
				})
				.AddTransient<MyService>()
				.BuildServiceProvider();

			FileLoggerOptions fileLoggerOptions = serviceProvider.GetRequiredService<IOptions<FileLoggerOptions>>().Value;

			IFileLogSink fileLogSink = serviceProvider.GetRequiredService<IFileLogSink>();

			fileLogSink.Start(fileLoggerOptions);

			ILogger<Program> programLogger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

			programLogger.LogInformation("started");

			programLogger.LogTrace("trace");
			programLogger.LogDebug("debug");
			programLogger.LogInformation("information");
			programLogger.LogWarning("warning");
			programLogger.LogError("error");
			programLogger.LogCritical("critical");

			int messagesToWrite = 10;

			MyService myService = serviceProvider.GetRequiredService<MyService>();

			for (int i = 1; i < messagesToWrite + 1; i++)
			{
				myService.DoStuff($"fred {i}");

				if (i % 7 == 0)
				{
					programLogger.LogCritical($"i am a critical message from program logger");
				}

				await Task.Delay(TimeSpan.FromMilliseconds(100d));
			}

			programLogger.LogInformation("ended");

			return 0;
		}
	}

	public class MyService
	{
		private readonly ILogger<MyService> logger;

		public MyService(ILogger<MyService> logger)
		{
			this.logger = logger;
		}

		public void DoStuff(string message)
		{
			logger.LogDebug(message);
		}
	}
}
