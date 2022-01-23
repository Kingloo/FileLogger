using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public static class FileLoggerExtensions
	{
		public static ILoggingBuilder AddFileLogger(this ILoggingBuilder loggingBuilder, IConfigurationSection configurationSection)
		{
			loggingBuilder.Services.AddOptions<FileLoggerOptions>().Configure(options =>
			{
				options.Path = configurationSection["Path"];
				options.LogLevel = Enum.Parse<LogLevel>(configurationSection["LogLevel"]);
				options.TimestampFormat = configurationSection["TimestampFormat"];
				options.UseUtcTimestamp = bool.Parse(configurationSection["UseUtcTimestamp"]);
				options.DrainIntervalMs = Int32.Parse(configurationSection["DrainIntervalMs"]);
				options.DrainCount = Int32.Parse(configurationSection["DrainCount"]);
			});

			loggingBuilder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

			return loggingBuilder;
		}

		public static ILoggingBuilder AddFileLogger(this ILoggingBuilder loggingBuilder, Action<FileLoggerOptions> configure)
		{
			loggingBuilder.Services.AddOptions<FileLoggerOptions>()
				.Configure(configure);

			loggingBuilder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

			return loggingBuilder;
		}
	}
}
