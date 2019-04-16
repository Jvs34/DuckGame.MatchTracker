using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//hats are used to define teams in duck game, so we kinda do need to track them
	public class TeamData : IPlayersList, IEquatable<TeamData>, IComparable<TeamData>
	{
		public bool hasHat { get; set; }
		public string hatName { get; set; }
		public bool isCustomHat { get; set; }
		public List<PlayerData> players { get; set; } = new List<PlayerData>();
		public int score { get; set; }

		public int CompareTo( TeamData other )
		{
			return score.CompareTo( other.score );
		}

		public bool Equals( TeamData other )
		{
			return hatName == other.hatName;
		}
	}
}