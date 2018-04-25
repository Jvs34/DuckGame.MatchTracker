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
		//for the json stuff I might turn this into a string list
		List<Profile> players = new List<Profile>();

		//same as above, dunno how the json library deals with duplicated data
		List<RoundData> rounds = new List<RoundData>();
		
		//when the match started
		DateTime timeStarted;
		
		//when the match ended, this might actually be invalid if the match is never completed, like when the game crashes before the end
		//although then the match data would never be written in the first place
		DateTime timeEnded;

		//might be null if uh the match is never completed I guess
		Profile matchWinner;
	}

    class RoundData
    {
		//while it seems unnecessary to have a list of players here, duck game now supports disconnections without interrupting gameplay
		//so someone might be there for one round and be gone on the other
		List<Profile> players = new List<Profile>();

		String level;

		//can be null, happens a lot of course
		Profile winner;
		
		DateTime timeStarted;
		DateTime timeEnded;
    }


	//duck game networked profiles aren't all that networked really, you only get the name and id
	class Profile
	{
		//usually the steamid, if this is a localplayer it will be PROFILE1/2/3/4 whatever
		String userid;
		//name of the user
		String name;

	}
}
