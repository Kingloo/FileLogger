using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileLogger;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AspNetCoreSample
{
	public class Program
	{
		private static readonly IFileLogSink sink = new FileLogSink();
		private static readonly CancellationTokenSource cts = new CancellationTokenSource();

		public Program()
		{
			Console.CancelKeyPress += (s, e) =>
			{
				Console.WriteLine("ctrl-c pressed, exiting");

				cts.Cancel();
			};
		}

		public static async Task<int> Main(string[] args)
		{
			IHost host = BuildHost();

			try
			{
				await host.RunAsync(cts.Token);
			}
			finally
			{
				await sink.DisposeAsync();

				cts.Dispose();
			}

			return 0;
		}

		private static IHost BuildHost()
		{
			return new HostBuilder()
				.ConfigureHostConfiguration(ConfigureHostConfiguration)
				.ConfigureAppConfiguration(ConfigureAppConfiguration)
				.ConfigureServices(ConfigureServices)
				.ConfigureLogging(ConfigureLogging)
				.ConfigureWebHost(ConfigureWebHost)
				.Build();
		}

		private static void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
		{
			configurationBuilder.AddCommandLine(Environment.GetCommandLineArgs());
		}

		private static void ConfigureAppConfiguration(HostBuilderContext ctx, IConfigurationBuilder configurationBuilder)
		{
			configurationBuilder.AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: false);
		}

		private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton(sink);
		}

		private static void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder loggingBuilder)
		{
			loggingBuilder.AddConfiguration(ctx.Configuration.GetSection("Logging"));

			loggingBuilder.AddSimpleConsole(simpleConsoleOptions =>
			{
				simpleConsoleOptions.ColorBehavior = LoggerColorBehavior.Enabled;
				simpleConsoleOptions.TimestampFormat = ctx.Configuration["FileLogger.TimestampFormat"];
				simpleConsoleOptions.SingleLine = true;
			});

			loggingBuilder.AddFileLogger(fileLoggerOptions =>
			{
				var fileLoggerOptionsConfigurationSection = ctx.Configuration.GetSection("FileLoggerOptions");

				fileLoggerOptions.Path = fileLoggerOptionsConfigurationSection["Path"];
				fileLoggerOptions.LogLevel = Enum.Parse<LogLevel>(fileLoggerOptionsConfigurationSection["LogLevel"]);
				fileLoggerOptions.TimestampFormat = fileLoggerOptionsConfigurationSection["TimestampFormat"];
				fileLoggerOptions.UseUtcTimestamp = bool.Parse(fileLoggerOptionsConfigurationSection["UseUtcTimestamp"]);
				fileLoggerOptions.DrainInterval = TimeSpan.FromMilliseconds(double.Parse(fileLoggerOptionsConfigurationSection["DrainInterval"]));
				fileLoggerOptions.DrainCount = Int32.Parse(fileLoggerOptionsConfigurationSection["DrainCount"]);
			});
		}

		private static void ConfigureWebHost(IWebHostBuilder webHostBuilder)
		{
			webHostBuilder
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseKestrel((ctx, kestrelOptions) =>
				{
					kestrelOptions.Configure(ctx.Configuration.GetSection("Kestrel"));
				})
				.UseStartup<Startup>();
		}
	}
}
