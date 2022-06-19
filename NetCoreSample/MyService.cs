using System;
using Microsoft.Extensions.Logging;

namespace NetCoreSample
{
	public class MyService
	{
		private readonly ILogger<MyService> logger;

		public MyService(ILogger<MyService> logger)
		{
			if (logger is null)
			{
				throw new ArgumentNullException(nameof(logger));
			}

			this.logger = logger;
		}

		public void DoStuff(string message)
		{
			if (String.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message));
			}

			logger.LogInformation(message);
		}
	}
}
