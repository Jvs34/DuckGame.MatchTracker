using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public class RoundData : IPlayersList, IStartEnd, IWinner, IVideoUpload, IEquatable<RoundData>, IComparable<RoundData> , ITagsList , IDatabaseEntry
	{
		public RecordingType RecordingType { get; set; }
		public bool IsCustomLevel { get; set; }

		//id of the level this round was played on
		//unfortunately the actual path of the level is already gone by the time this is available
		public string LevelName { get; set; }
		public string Name { get; set; }
		public string MatchName { get; set; }

		public string DatabaseIndex => Name;

		public List<PlayerData> Players { get; set; } = new List<PlayerData>();
		public List<TeamData> Teams { get; set; } = new List<TeamData>();
		public DateTime TimeEnded { get; set; }
		public DateTime TimeStarted { get; set; }
		public TeamData Winner { get; set; }

		//youtube url id of this round, this will be null by default, then filled by the uploader before being stored away
		public string YoutubeUrl { get; set; }
		public VideoType VideoType { get; set; } = VideoType.VideoLink;
		public List<VideoMirrorData> VideoMirrors { get; set; } = new List<VideoMirrorData>();
		public DatabaseEntries<TagData> Tags { get; set; } = new DatabaseEntries<TagData>();

		public int CompareTo( RoundData other )
		{
			return TimeStarted.CompareTo( other.TimeStarted );
		}

		public bool Equals( RoundData other )
		{
			return Name == other.Name;
		}

		public TimeSpan GetDuration()
		{
			return TimeEnded.Subtract( TimeStarted );
		}

		public string GetWinnerName()
		{
			string winnerName = "";
			var winners = GetWinners();
			//check if anyone actually won
			if( winners.Count != 0 )
			{
				winnerName = winners.Count > 1 ? Winner.HatName : winners [0].GetName();
			}

			return winnerName;
		}

		public List<PlayerData> GetWinners()
		{
			return Winner?.Players ?? new List<PlayerData>();
		}
	}
}