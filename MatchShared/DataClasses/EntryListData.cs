using System.Collections.Generic;

namespace MatchTracker
{
	/// <summary>
	/// Lists the entries currently present in the database to avoid expensive lookups
	/// </summary>
	public class EntryListData : IDatabaseEntry
	{
		public string Type { get; set; }
		public string DatabaseIndex => Type;
		public List<string> Entries { get; set; } = new List<string>();
	}
}
