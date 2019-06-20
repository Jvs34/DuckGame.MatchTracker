using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//hats are used to define teams in duck game, so we kinda do need to track them
	public class TeamData : IPlayersList, IEquatable<TeamData>, IComparable<TeamData>
	{
		public bool HasHat { get; set; }
		public string HatName { get; set; }
		public bool IsCustomHat { get; set; }
		public List<string> Players { get; set; } = new List<string>();
		public int Score { get; set; }

		public int CompareTo( TeamData other )
		{
			return Score.CompareTo( other.Score );
		}

		public bool Equals( TeamData other )
		{
			return HatName == other.HatName;
		}

		public override string ToString()
		{
			return $"{HatName} +{Score} " + string.Join( "," , Players );
		}
	}
}