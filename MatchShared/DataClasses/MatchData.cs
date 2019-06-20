using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//a match is kind of hard to keep track of in a sense, reconnections might throw stats off and create duplicate matches
	//which in theory is fine until you want to link multiple matches together later on, gotta think about this
	public class MatchData : IPlayersList, IRoundsList, IStartEnd, IWinner, IVideoUpload, IEquatable<MatchData>, IComparable<MatchData> , ITagsList , IDatabaseEntry
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

		public string YoutubeUrl { get; set; } = string.Empty;
		public VideoType VideoType { get; set; } = VideoType.PlaylistLink;
		public List<VideoMirrorData> VideoMirrors { get; set; } = new List<VideoMirrorData>();
		public List<string> Tags { get; set; } = new List<string>();

		public int CompareTo( MatchData other )
		{
			return TimeStarted.CompareTo( other.TimeStarted );
		}

		public bool Equals( MatchData other )
		{
			return Name == other.Name;
		}

		public TimeSpan GetDuration()
		{
			return TimeEnded.Subtract( TimeStarted );
		}

		public List<string> GetWinners()
		{
			return Winner?.Players ?? new List<string>();
		}
	}
}