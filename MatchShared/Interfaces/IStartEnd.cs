using System;

namespace MatchTracker
{
	public interface IStartEnd
	{
		string name { get; set; }

		DateTime timeEnded { get; set; }
		DateTime timeStarted { get; set; }

		TimeSpan GetDuration();
	}
}