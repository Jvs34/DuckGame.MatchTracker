using System.Collections.Generic;

namespace MatchTracker
{
	//TODO
	public class LevelData : ITags
	{
		/// <summary>
		/// The level's guid
		/// </summary>
		public string LevelName { get; set; }

		/// <summary>
		/// The level's path on the filesystem
		/// </summary>
		public string FilePath { get; set; }

		/// <summary>
		/// Whether this is a map loaded from the workshop or sent over the network
		/// </summary>
		public bool IsCustomMap { get; set; }

		public List<TagData> Tags { get; set; } = new List<TagData>();
	}
}