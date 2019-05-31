using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public class TagData : IDatabaseEntry
	{
		/// <summary>
		/// Used for id of the emoji, stuff like :eggplant:
		/// Debating whether or not to make it contain the colons in it
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Unicode(?) of the emoji, stuff like 🍆
		/// </summary>
		public string Emoji { get; set; }

		public string DatabaseIndex => Name;
	}
}
