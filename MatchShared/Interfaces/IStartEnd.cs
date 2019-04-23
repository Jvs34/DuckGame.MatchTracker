using System;

namespace MatchTracker
{
	public interface IStartEnd
	{
		string Name { get; set; }

		DateTime TimeEnded { get; set; }
		DateTime TimeStarted { get; set; }

		TimeSpan GetDuration();
	}
}