using System;

namespace MatchTracker
{
	public class ReplayData : IStartEnd
	{
		public string name { get; set; }

		public DateTime timeEnded { get; set; }
		public DateTime timeStarted { get; set; }

		public TimeSpan GetDuration()
		{
			return timeEnded.Subtract( timeStarted );
		}
	}
}