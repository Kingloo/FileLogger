using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace FileLogger
{
	public static class FileLoggerExtensions
	{
		public static ILoggingBuilder AddFileLogger(this ILoggingBuilder loggingBuilder, Action<FileLoggerOptions> configure)
		{
			LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(loggingBuilder.Services);

			loggingBuilder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

			loggingBuilder.Services.Configure(configure);

			return loggingBuilder;
		}
	}
}
