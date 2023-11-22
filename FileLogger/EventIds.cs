using System;
using System.Collections.Generic;

namespace FileLogger
{
	public static class EventIds
	{
		public const int Disposed = 0;
		public const int Timer = 1;
		public const int OptionsUpdated = 2;

		public static bool ShouldLogFileLoggerEventId(IList<int> eventIds, int eventId)
		{
			ArgumentNullException.ThrowIfNull(eventIds);
			ArgumentOutOfRangeException.ThrowIfLessThan<int>(eventId, 0);

			if (eventIds.Count == 0)
			{
				return false;
			}

			if (eventIds.Count == 1
				&& eventIds[0] == -1)
			{
				return true;
			}

			return eventIds.Contains(eventId);
		}
	}
}
