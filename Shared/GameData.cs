using System;
using System.Collections.Generic;

namespace MatchTracker
{

	//this is what accessed by the website, so it will list the name of the matches that were tracked
	public class GlobalData : IPlayersList , IMatchesList , IRoundsList
	{
		public List<String> matches { get; set; }

		//unfortunately sometimes duck game might crash because someone still hasn't fixed the end game screen crash so the match might not get saved
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
	public class MatchData : IPlayersList , IRoundsList , IStartEnd , IWinner
	{
		//name of the match
		public String matchName;

		public List<PlayerData> players { get; set; }

		//filename(without extension) of the rounds of this match
		public List<String> rounds { get; set; }

		//when the match started, this is also used as the name for the file
		public DateTime timeStarted { get; set; }

		//this might actually be invalid if the match is never completed, like when the game crashes before the end
		//although then the match data would never be written in the first place
		public DateTime timeEnded { get; set; }

		//might be null if uh the match is never completed I guess
		public TeamData winner { get; set; }

		public MatchData()
		{
			players = new List<PlayerData>();
			rounds = new List<string>();
		}

		public TimeSpan GetDuration()
		{
			return timeEnded.Subtract( timeStarted );
		}

		public String GetWinnerName()
		{
			String winnerName = "";

			//check if anyone actually won
			if( winner != null )
			{
				var winners = players.FindAll( p => p.team.hatName == winner.hatName );
				if( winners.Count > 1 )
				{
					winnerName = winner.hatName;
				}
				else
				{
					winnerName = winners [0].GetName();
				}

			}

			return winnerName;
		}
	}

	public class RoundData : IPlayersList , IStartEnd , IWinner
	{
		public String roundName;

		//while it seems unnecessary to have a list of players here, duck game now supports disconnections without interrupting gameplay
		//so someone might be there for one round and be gone on the other
		public List<PlayerData> players { get; set; }

		//id of the level this round was played on
		//unfortunately the actual path of the level is already gone by the time this is available
		public String levelName;

		//useful to filter out
		public bool isCustomLevel;
		public bool skipped;

		//can be null, happens a lot of course if everyone dies
		public TeamData winner { get; set; }

		public DateTime timeStarted { get; set; }
		public DateTime timeEnded { get; set; }

		//youtube url id of this round, this will be null by default, then filled by the uploader before being stored away
		public String youtubeUrl;

		public RoundData()
		{
			players = new List<PlayerData>();
		}

		public TimeSpan GetDuration()
		{
			return timeEnded.Subtract( timeStarted );
		}

		public String GetWinnerName()
		{
			String winnerName = "";

			//check if anyone actually won
			if( winner != null )
			{
				var winners = players.FindAll( p => p.team.hatName == winner.hatName );
				if( winners.Count > 1 )
				{
					winnerName = winner.hatName;
				}
				else
				{
					winnerName = winners [0].GetName();
				}

			}

			return winnerName;
		}
	}


	//duck game networked profiles aren't all that networked really, you only get the name and id
	public class PlayerData
	{
		//usually the steamid, if this is a localplayer it will be PROFILE1/2/3/4 whatever
		public String userId;
		//name of the user
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
	public interface IWinner
	{
		TeamData winner { get; set; }

		String GetWinnerName();
	}

	public interface IStartEnd
	{
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
}
