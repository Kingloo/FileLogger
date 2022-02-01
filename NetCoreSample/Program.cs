using System;
using System.Threading.Tasks;
using FileLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace NetCoreSample
{
	public class Program
	{
		public static async Task<int> Main(string[] args)
		{
			string env = Environment.GetEnvironmentVariable("Environment") ?? "Production";

			await using IFileLoggerSink sink = new FileLoggerSink();

			IServiceProvider serviceProvider = new ServiceCollection()
				.AddSingleton(sink)
				.AddLogging(logging =>
				{
					logging.SetMinimumLevel(LogLevel.Debug);

					logging.AddSimpleConsole(simpleConsoleOptions =>
					{
						simpleConsoleOptions.ColorBehavior = LoggerColorBehavior.Enabled;
						simpleConsoleOptions.TimestampFormat = $"[{FileLoggerOptions.DefaultTimestampFormat}] ";
						simpleConsoleOptions.SingleLine = true;
					});

					logging.AddFileLogger(options =>
					{
						options.Path = @".\test.txt";
						options.LogEventIds = new int[] { LogEventIds.Timer };
					});
				})
				.AddTransient<MyService>()
				.BuildServiceProvider();

			FileLoggerOptions fileLoggerOptions = serviceProvider.GetRequiredService<IOptions<FileLoggerOptions>>().Value;

			IFileLoggerSink fileLogSink = serviceProvider.GetRequiredService<IFileLoggerSink>();

			fileLogSink.Start(fileLoggerOptions);

			ILogger<Program> programLogger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

			programLogger.LogInformation("started");

			programLogger.LogTrace(1, "trace");
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
}
