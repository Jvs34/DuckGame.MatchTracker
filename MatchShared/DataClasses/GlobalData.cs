using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//this is what accessed by the website, so it will list the name of the matches that were tracked
	public class GlobalData : IDatabaseEntry, IPlayersList
	{
		public string DatabaseIndex => nameof( GlobalData );
		public List<PlayerData> Players { get; set; } = new List<PlayerData>();
	}
}