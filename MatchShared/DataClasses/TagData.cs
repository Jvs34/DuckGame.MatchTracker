using System;

namespace MatchTracker
{
	public class TagData : IDatabaseEntry
	{
		/// <summary>
		/// Used for id of the emoji
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Unicode(?) of the emoji, stuff like 🍆
		/// </summary>
		public string Emoji { get; set; }

		/// <summary>
		/// Stuff like :joy:
		/// </summary>
		public string FancyName { get; set; }

		public string DatabaseIndex => Name;
	}
}
