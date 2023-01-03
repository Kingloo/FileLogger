using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using FileLogger;

namespace AspNetCoreSample
{
	public class Program
	{
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
			bool didRunCleanly = false;

			IHost host = BuildHost();

			try
			{
				await host.RunAsync(cts.Token);

				didRunCleanly = true;
			}
			finally
			{
				cts.Dispose();
			}

			return didRunCleanly ? 0 : -1;
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
			configurationBuilder
				.AddCommandLine(Environment.GetCommandLineArgs())
				.AddEnvironmentVariables()
				.SetBasePath(RuntimeCircumstance.GetRealLocation());
		}

		private static void ConfigureAppConfiguration(HostBuilderContext ctx, IConfigurationBuilder configurationBuilder)
		{
			string folder = RuntimeCircumstance.GetRealLocation();

			string appsettingsFilename = Path.Combine(folder, "appsettings.json");
			string appsettingsEnvironmentFilename = Path.Combine(folder, $"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json");

			if (File.Exists(appsettingsFilename))
			{
				configurationBuilder.AddJsonFile(
					appsettingsFilename,
					optional: false,
					reloadOnChange: true);
			}
			else if (File.Exists(appsettingsEnvironmentFilename))
			{
				configurationBuilder.AddJsonFile(
					appsettingsEnvironmentFilename,
					optional: false,
					reloadOnChange: true);
			}
			else
			{
				throw new FileNotFoundException("failed to find either appsettings file");
			}
		}

		private static void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder loggingBuilder)
		{
			loggingBuilder.AddConfiguration(ctx.Configuration.GetSection("Logging"));

			loggingBuilder.AddSimpleConsole(simpleConsoleOptions =>
			{
				simpleConsoleOptions.ColorBehavior = LoggerColorBehavior.Enabled;
				simpleConsoleOptions.SingleLine = true;
			});

			loggingBuilder.AddFileLogger();
		}

		private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IFileLoggerSink, FileLoggerSink>();

			services.Configure<FileLoggerOptions>(ctx.Configuration.GetSection("FileLogger"));
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
