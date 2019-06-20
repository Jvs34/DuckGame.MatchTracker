using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public class EntryListData : IDatabaseEntry
	{
		public string Type { get; set; }
		public string DatabaseIndex => Type;

		public List<string> Entries { get; set; } = new List<string>();
	}
}
