using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//this is what accessed by the website, so it will list the name of the matches that were tracked
	public class GlobalData : IPlayersList, IMatchesList, IRoundsList, ITagsList, IDatabaseEntry, ILevelList
	{
		public string DatabaseIndex => nameof( GlobalData );
		public List<string> Matches { get; set; } = new List<string>();
		public List<PlayerData> Players { get; set; } = new List<PlayerData>();
		public List<string> Rounds { get; set; } = new List<string>();
		public List<string> Tags { get; set; } = new List<string>();
		public List<string> Levels { get; set; } = new List<string>();
	}
}