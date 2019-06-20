using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//this is what accessed by the website, so it will list the name of the matches that were tracked
	public class GlobalData : IPlayersList, IMatchesList, IRoundsList, ITagsList, IDatabaseEntry, ILevelList
	{
		public string DatabaseIndex => nameof( GlobalData );
		public DatabaseEntries<MatchData> Matches { get; set; } = new DatabaseEntries<MatchData>();
		public List<PlayerData> Players { get; set; } = new List<PlayerData>();
		public DatabaseEntries<RoundData> Rounds { get; set; } = new DatabaseEntries<RoundData>();
		public DatabaseEntries<TagData> Tags { get; set; } = new DatabaseEntries<TagData>();
		public DatabaseEntries<LevelData> Levels { get; set; } = new DatabaseEntries<LevelData>();
	}
}