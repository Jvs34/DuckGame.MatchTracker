namespace MatchTracker
{
	//TODO
	public class LevelData
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
	}
}