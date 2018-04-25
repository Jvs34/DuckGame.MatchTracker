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
	class MatchData
	{
		//for the json stuff I might turn this into a string list to prevent duplicated data
		List<PlayerData> players = new List<PlayerData>();

		//same as above, dunno how the json library deals with duplicated data
		List<RoundData> rounds = new List<RoundData>();
		
		//when the match started
		DateTime timeStarted;
		
		//this might actually be invalid if the match is never completed, like when the game crashes before the end
		//although then the match data would never be written in the first place
		DateTime timeEnded;

		//might be null if uh the match is never completed I guess
		PlayerData matchWinner;
	}

    class RoundData
    {
		//while it seems unnecessary to have a list of players here, duck game now supports disconnections without interrupting gameplay
		//so someone might be there for one round and be gone on the other
		List<PlayerData> players = new List<PlayerData>();

		//name of the level this round was played on
		String levelName;

		//useful to filter out
		bool isWorkshopLevel;

		//can be null, happens a lot of course
		PlayerData winner;
		
		DateTime timeStarted;
		DateTime timeEnded;

		//youtube url of this round, this will be null by default, then filled by the uploader before being stored away
		String youtubeUrl;
    }


	//duck game networked profiles aren't all that networked really, you only get the name and id
	class PlayerData
	{
		//usually the steamid, if this is a localplayer it will be PROFILE1/2/3/4 whatever
		String userid;
		//name of the user
		String name;

		//custom nickname for the player, might happen in the case of some cunt changing name to a shitty one all the time *cough* raptor *cough*
		String nickName;
	}
}
