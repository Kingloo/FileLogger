using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileLogger
{
	public static class FileLoggerExtensions
	{
		public static ILoggingBuilder AddFileLogger(this ILoggingBuilder loggingBuilder, Action<FileLoggerOptions> configure)
		{
			loggingBuilder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

			loggingBuilder.Services.Configure(configure);

			return loggingBuilder;
		}
	}
}
