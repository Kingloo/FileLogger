using System.Linq;

namespace FileLogger
{
	public static class LogEventIds
	{
		public const int Disposed = 0;
		public const int Timer = 1;
		public const int OptionsUpdated = 2;

		public static bool ShouldLogFileLoggerEventId(int[] eventIds, int eventId)
		{
			if (eventIds.Length == 0)
			{
				return false;
			}

			if (eventIds.Length == 1
				&& eventIds[0] == -1)
			{
				return true;
			}

			return eventIds.Contains(eventId);
		}
	}
}
