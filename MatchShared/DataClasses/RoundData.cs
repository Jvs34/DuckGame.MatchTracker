using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public class RoundData : IPlayersList, IStartEnd, IWinner, IYoutube, IEquatable<RoundData>, IComparable<RoundData>
	{
		public RecordingType recordingType;
		public bool isCustomLevel { get; set; }

		//id of the level this round was played on
		//unfortunately the actual path of the level is already gone by the time this is available
		public String levelName { get; set; }

		public String name { get; set; }
		public String matchName { get; set; }
		public virtual List<PlayerData> players { get; set; } = new List<PlayerData>();
		public bool skipped { get; set; }
		public List<TeamData> teams { get; set; } = new List<TeamData>();
		public DateTime timeEnded { get; set; }
		public DateTime timeStarted { get; set; }
		public virtual TeamData winner { get; set; }

		//youtube url id of this round, this will be null by default, then filled by the uploader before being stored away
		public String youtubeUrl { get; set; }
		public VideoType videoType { get; set; } = VideoType.VideoLink;

		public int CompareTo( RoundData other )
		{
			return timeStarted.CompareTo( other.timeStarted );
		}

		public bool Equals( RoundData other )
		{
			return name == other.name;
		}

		public TimeSpan GetDuration()
		{
			return timeEnded.Subtract( timeStarted );
		}

		public String GetWinnerName()
		{
			String winnerName = "";
			var winners = GetWinners();
			//check if anyone actually won
			if( winners.Count != 0 )
			{
				winnerName = winners.Count > 1 ? winner.hatName : winners [0].GetName();
			}

			return winnerName;
		}

		public List<PlayerData> GetWinners()
		{
			return winner?.players ?? new List<PlayerData>();
		}
	}
}