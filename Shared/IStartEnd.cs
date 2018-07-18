using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public interface IStartEnd
	{
		String name { get; set; }

		DateTime timeStarted { get; set; }
		DateTime timeEnded { get; set; }

		TimeSpan GetDuration();
	}
}
