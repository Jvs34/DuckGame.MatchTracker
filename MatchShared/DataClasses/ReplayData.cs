using System;

namespace MatchTracker
{
	public class ReplayData : IStartEnd
	{
		public string Name { get; set; }

		public DateTime TimeEnded { get; set; }
		public DateTime TimeStarted { get; set; }

		public ReplayRecording Recording { get; set; }

		public TimeSpan GetDuration()
		{
			return TimeEnded.Subtract( TimeStarted );
		}
	}
}