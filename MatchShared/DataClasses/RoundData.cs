using MatchTracker.Interfaces;
using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public class RoundData : IPlayersList, IKillList, IStartEnd, IWinner, IVideoUploadList, ITagsList, ILevelName, IDatabaseEntry
	{
		public RecordingType RecordingType { get; set; }
		public string LevelName { get; set; }
		public string Name { get; set; }
		public string MatchName { get; set; }
		public string DatabaseIndex => Name;
		public List<string> Players { get; set; } = new List<string>();
		public List<TeamData> Teams { get; set; } = new List<TeamData>();
		public List<KillData> KillsList { get; set; } = new List<KillData>();
		public DateTime TimeEnded { get; set; }
		public DateTime TimeStarted { get; set; }
		public TeamData Winner { get; set; }
		public List<VideoUpload> VideoUploads { get; set; } = new List<VideoUpload>();
		public List<string> Tags { get; set; } = new List<string>();

		public TimeSpan GetDuration() => TimeEnded.Subtract( TimeStarted );
		public List<string> GetWinners() => Winner?.Players ?? new List<string>();
	}
}