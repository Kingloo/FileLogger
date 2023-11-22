using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace FileLogger
{
	public static class FileLoggerExtensions
	{
		public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);

			builder.AddConfiguration();

			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

			LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);

			return builder;
		}

		public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerOptions> configure)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(configure);

			builder.AddFileLogger();

			builder.Services.Configure(configure);

			return builder;
		}
	}
}
