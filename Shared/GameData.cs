using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//this is what accessed by the website, so it will list the name of the matches that were tracked
	public class GlobalData : IPlayersList, IMatchesList, IRoundsList
	{
		public List<String> matches { get; set; }

		public List<String> rounds { get; set; }

		//all the players that have ever played any rounds, even local players
		//these player profiles will not have teamdata
		public List<PlayerData> players { get; set; }

		//TODO: level shit

		public GlobalData()
		{
			matches = new List<string>();
			rounds = new List<string>();
			players = new List<PlayerData>();
		}
	}

	//a match is kind of hard to keep track of in a sense, reconnections might throw stats off and create duplicate matches
	//which in theory is fine until you want to link multiple matches together later on, gotta think about this
	public class MatchData : IPlayersList, IRoundsList, IStartEnd, IWinner , IYoutube
	{
		//name of the match
		public String name { get; set; }

		public List<PlayerData> players { get; set; }

		public List<String> rounds { get; set; }

		public DateTime timeStarted { get; set; }
		public DateTime timeEnded { get; set; }

		public TeamData winner { get; set; }

		public String youtubeUrl { get; set; }

		public MatchData()
		{
			players = new List<PlayerData>();
			rounds = new List<string>();
		}

		public TimeSpan GetDuration()
		{
			return timeEnded.Subtract( timeStarted );
		}

		public List<PlayerData> GetWinners()
		{
			return winner != null ? players.FindAll( p => p.team.hatName == winner.hatName ) : new List<PlayerData>();
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
	}

	public class RoundData : IPlayersList, IStartEnd, IWinner , IYoutube
	{
		public String name { get; set; }

		public List<PlayerData> players { get; set; }

		//id of the level this round was played on
		//unfortunately the actual path of the level is already gone by the time this is available
		public String levelName;

		public bool isCustomLevel;
		public bool skipped;

		public DateTime timeStarted { get; set; }
		public DateTime timeEnded { get; set; }

		public TeamData winner { get; set; }

		//youtube url id of this round, this will be null by default, then filled by the uploader before being stored away
		public String youtubeUrl { get; set; }

		public RoundData()
		{
			players = new List<PlayerData>();
		}

		public TimeSpan GetDuration()
		{
			return timeEnded.Subtract( timeStarted );
		}

		public List<PlayerData> GetWinners()
		{
			return winner != null ? players.FindAll( p => p.team.hatName == winner.hatName ) : new List<PlayerData>();
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
	}

	//duck game networked profiles aren't all that networked really, you only get the name and id
	public class PlayerData
	{
		//usually the steamid, if this is a localplayer it will be PROFILE1/2/3/4 whatever
		public String userId;

		public String name;

		//custom nickname for the player, this will be set manually on another json
		public String nickName;

		//yes a hat is a team
		public TeamData team;

		public String GetName()
		{
			return nickName ?? name;
		}
	}

	//hats are used to define teams in duck game, so we kinda do need to track them
	public class TeamData
	{
		public bool hasHat;
		public String hatName;
		public bool isCustomHat;
		public int score;
	}

	public interface IPlayersList
	{
		List<PlayerData> players { get; set; }
	}

	//should this inherit IPlayerList? a winner is always accompanied by a player list
	//this'll probably be more useful once we get interface traits in c# 8.0
	public interface IWinner : IPlayersList
	{
		TeamData winner { get; set; }
		List<PlayerData> GetWinners();
		String GetWinnerName();
	}

	public interface IStartEnd
	{
		String name { get; set; }

		DateTime timeStarted { get; set; }
		DateTime timeEnded { get; set; }

		TimeSpan GetDuration();
	}

	public interface IMatchesList
	{
		List<String> matches { get; set; }
	}

	public interface IRoundsList
	{
		List<String> rounds { get; set; }
	}

	public interface IYoutube
	{
		String youtubeUrl { get; set; }
	}
}
