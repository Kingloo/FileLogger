using Microsoft.Extensions.Logging;

namespace NetCoreSample
{
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
