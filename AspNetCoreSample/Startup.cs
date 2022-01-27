using System;
using System.Net;
using System.Text;
using FileLogger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreSample
{
	public class Startup
	{
		public IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddRouting();

			services.AddTransient<MyService>();
		}

		public void Configure(
			IApplicationBuilder app,
			IWebHostEnvironment env,
			ILoggerFactory loggerFactory,
			IServiceProvider serviceProvider)
		{
			var sink = serviceProvider.GetRequiredService<IFileLoggerSink>();
			var fileLoggerOptions = serviceProvider.GetRequiredService<IOptions<FileLoggerOptions>>().Value;

			sink.Start(fileLoggerOptions);

			var logger = loggerFactory.CreateLogger<Startup>();

			logger.LogInformation("startup started");

			logger.LogDebug("environment: {0}", env.EnvironmentName);

			if (env.IsDevelopment())
			{
				logger.LogTrace("using Developer exception page");

				app.UseDeveloperExceptionPage();
			}
			else
			{
				logger.LogTrace("NOT using Developer exception page");
			}

			app.UseRouting();

			app.Run(async ctx =>
			{
				ctx.Response.StatusCode = (int)HttpStatusCode.OK;

				await ctx.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("hello, world")).ConfigureAwait(false);
			});

			logger.LogInformation("startup finished");
		}
	}
}
