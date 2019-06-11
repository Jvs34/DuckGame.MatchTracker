using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public class TagData : IDatabaseEntry, IEquatable<TagData>
	{
		/// <summary>
		/// Used for id of the emoji, stuff like :eggplant: without the colons
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Unicode(?) of the emoji, stuff like 🍆
		/// </summary>
		public string Emoji { get; set; }

		public string DatabaseIndex => Name;

		public bool Equals( TagData other )
		{
			return Name.Equals( other.Name );
		}
	}
}
