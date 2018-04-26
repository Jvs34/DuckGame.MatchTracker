using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MatchTracker
{
	//a match is kind of hard to keep track of in a sense, reconnections might throw stats off and create duplicate matches
	//which in theory is fine until you want to link multiple matches together later on, gotta think about this
	public class MatchData
	{
		
		//for the json stuff I might turn this into a string list to prevent duplicated data
		//although it might be just fine to have
		public List<PlayerData> players;

		//filename(without extension) of the rounds of this match
		public List<String> rounds;

		//same as above, dunno how the json library deals with duplicated data
		//public List<RoundData> rounds = new List<RoundData>();

		//when the match started
		public DateTime timeStarted;

		//this might actually be invalid if the match is never completed, like when the game crashes before the end
		//although then the match data would never be written in the first place
		public DateTime timeEnded;

		//might be null if uh the match is never completed I guess
		public PlayerData matchWinner;
	}

	public class RoundData
	{
		//while it seems unnecessary to have a list of players here, duck game now supports disconnections without interrupting gameplay
		//so someone might be there for one round and be gone on the other
		public List<PlayerData> players;

		//name of the level this round was played on
		public String levelName;

		//useful to filter out
		public bool isCustomLevel;
		public bool skipped;

		//can be null, happens a lot of course if everyone dies
		public HatData winnerTeam;

		public DateTime timeStarted;
		public DateTime timeEnded;

		//youtube url of this round, this will be empty by default, then filled by the uploader before being stored away
		public String youtubeUrl;
	}


	//duck game networked profiles aren't all that networked really, you only get the name and id
	public class PlayerData
	{
		//usually the steamid, if this is a localplayer it will be PROFILE1/2/3/4 whatever
		public String userId;
		//name of the user
		public String name;

		//custom nickname for the player, might happen in the case of some cunt changing name to a shitty one all the time *cough* raptor *cough*
		public String nickName;

		//yes a hat is a team
		public HatData team;
	}

	//hats are used to define teams in duck game, so we kinda do need to track them
	public class HatData
	{
		public String hatName;
		public bool isCustomHat;
	}
}
