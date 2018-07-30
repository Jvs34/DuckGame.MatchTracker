using System;

namespace MatchTracker
{
	//hats are used to define teams in duck game, so we kinda do need to track them
	public class TeamData : IEquatable<TeamData>, IComparable<TeamData>
	{
		public bool hasHat;
		public String hatName;
		public bool isCustomHat;
		public int score;

		public bool Equals( TeamData other )
		{
			return hatName == other.hatName;
		}

		public int CompareTo( TeamData other )
		{
			return score.CompareTo( other.score );
		}

	}
}
