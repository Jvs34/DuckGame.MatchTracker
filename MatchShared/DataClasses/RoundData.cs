using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public sealed class RoundData : IPlayersList, IStartEnd, IWinner, IYoutube, IEquatable<RoundData>, IComparable<RoundData>
	{
		public RecordingType recordingType;
		public bool isCustomLevel { get; set; }

		//id of the level this round was played on
		//unfortunately the actual path of the level is already gone by the time this is available
		public String levelName { get; set; }

		public String name { get; set; }

		public List<PlayerData> players { get; set; }
		public bool skipped { get; set; }

		public DateTime timeEnded { get; set; }
		public DateTime timeStarted { get; set; }
		public TeamData winner { get; set; }

		//youtube url id of this round, this will be null by default, then filled by the uploader before being stored away
		public String youtubeUrl { get; set; }

		public RoundData()
		{
			players = new List<PlayerData>();
		}

		public int CompareTo( RoundData other )
		{
			return timeStarted.CompareTo( other.timeStarted );
		}

		public bool Equals( RoundData other )
		{
			return name == other.name && youtubeUrl == other.youtubeUrl;
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
			return winner != null ? players.FindAll( p => p.team.hatName == winner.hatName ) : new List<PlayerData>();
		}
	}
}