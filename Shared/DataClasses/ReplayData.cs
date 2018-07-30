using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public sealed class ReplayData : IStartEnd
	{
		public String name { get; set; }

		public DateTime timeStarted { get; set; }
		public DateTime timeEnded { get; set; }

		public TimeSpan GetDuration()
		{
			return timeEnded.Subtract( timeStarted );
		}
	}
}
