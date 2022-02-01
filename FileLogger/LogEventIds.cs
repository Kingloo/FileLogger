using System.Linq;

namespace FileLogger
{
	internal static class LogEventIds
	{
		internal const int Timer = 0;
		internal const int Disposed = 1;
		internal const int OptionsUpdated = 2;

		internal static bool ShouldLogEventId(int[] eventIds, int eventId)
		{
			if (eventIds.Length == 0)
			{
				return true;
			}

			return eventIds.Contains(eventId);
		}
	}
}
