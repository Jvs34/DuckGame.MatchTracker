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
}
