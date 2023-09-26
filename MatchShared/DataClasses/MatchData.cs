using System;
using System.Collections.Generic;

namespace MatchTracker
{
	/// <summary>
	/// A match does not necessarily start when the first round starts or the last round ends,<para/>
	/// that depends entirely when Duck Game considers a game started or concluded, so keep that in mind when using dates
	/// </summary>
	public class MatchData : IPlayersList, IRoundsList, IStartEndTime, IWinner, IVideoUploadList, ITagsList, IDatabaseEntry
	{
		public string Name { get; set; } = string.Empty;
		public string DatabaseIndex => Name;
		public List<string> Players { get; set; } = new List<string>();
		public List<string> Rounds { get; set; } = new List<string>();
		public List<TeamData> Teams { get; set; } = new List<TeamData>();
		public DateTime TimeEnded { get; set; }
		public DateTime TimeStarted { get; set; }
		public TeamData Winner { get; set; }
		public List<VideoUpload> VideoUploads { get; set; } = new List<VideoUpload>();
		public List<string> Tags { get; set; } = new List<string>();

		public TimeSpan GetDuration() => TimeEnded.Subtract( TimeStarted );
		public List<string> GetWinners() => Winner?.Players ?? new List<string>();
	}
}