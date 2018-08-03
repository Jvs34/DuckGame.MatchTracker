using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//a match is kind of hard to keep track of in a sense, reconnections might throw stats off and create duplicate matches
	//which in theory is fine until you want to link multiple matches together later on, gotta think about this
	public sealed class MatchData : IPlayersList, IRoundsList, IStartEnd, IWinner, IYoutube, IEquatable<MatchData>, IComparable<MatchData>
	{
		//name of the match
		public String name { get; set; }

		public List<PlayerData> players { get; set; }

		public List<String> rounds { get; set; }

		public DateTime timeEnded { get; set; }
		public DateTime timeStarted { get; set; }
		public TeamData winner { get; set; }

		public String youtubeUrl { get; set; }

		public MatchData()
		{
			players = new List<PlayerData>();
			rounds = new List<string>();
		}

		public int CompareTo( MatchData other )
		{
			return timeStarted.CompareTo( other.timeStarted );
		}

		public bool Equals( MatchData other )
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