using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	/// <summary>
	/// Matches played back to back in the same session, this is tracked automatically
	/// </summary>
	public class TournamentData : IWinner, IMatchesList, IDatabaseEntry, IStartEndTime, ITagsList
	{
		public string Name { get; set; }
		public string DatabaseIndex => Name;
		public List<string> Matches { get; set; } = new List<string>();
		public DateTime TimeStarted { get; set; }
		public DateTime TimeEnded { get; set; }
		public TeamData Winner { get; set; }
		public List<string> Players { get; set; } = new List<string>();
		public List<TeamData> Teams { get; set; } = new List<TeamData>();
		public List<string> Tags { get; set; } = new List<string>();

		public TimeSpan GetDuration() => TimeEnded.Subtract( TimeStarted );
		public List<string> GetWinners() => Winner?.Players ?? new List<string>();
	}
}
