using MatchTracker.Interfaces;
using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//a match is kind of hard to keep track of in a sense, reconnections might throw stats off and create duplicate matches
	//which in theory is fine until you want to link multiple matches together later on, gotta think about this
	public class MatchData : IPlayersList, IRoundsList, IStartEnd, IWinner, IVideoUploadList, ITagsList, IDatabaseEntry
	{
		//name of the match
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