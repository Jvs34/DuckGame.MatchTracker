using System;

namespace MatchTracker
{
	public interface IStartEnd
	{
		DateTime TimeEnded { get; set; }
		DateTime TimeStarted { get; set; }

		TimeSpan GetDuration();
	}
}