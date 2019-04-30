using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//this is what accessed by the website, so it will list the name of the matches that were tracked
	public class GlobalData : IPlayersList, IMatchesList, IRoundsList , ITags
	{
		public string Name => nameof( GlobalData );
		public List<string> Matches { get; set; } = new List<string>();

		/// <summary>
		/// all the players that have ever played any rounds, even local players
		/// these player profiles will not have teamdata
		/// </summary>
		public List<PlayerData> Players { get; set; } = new List<PlayerData>();
		public List<string> Rounds { get; set; } = new List<string>();

		public List<TagData> Tags { get; set; } = new List<TagData>();
	}
}