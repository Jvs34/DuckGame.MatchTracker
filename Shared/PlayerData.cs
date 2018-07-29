using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//duck game networked profiles aren't all that networked really, you only get the name and id
	public sealed class PlayerData : IEquatable<PlayerData>, IComparable<PlayerData>
	{
		//usually the steamid, if this is a localplayer it will be PROFILE1/2/3/4 whatever
		public String userId;

		public String name;

		//custom nickname for the player, this will be set manually on another json
		public String nickName;

		//yes a hat is a team
		public TeamData team;

		public ReplayData replay;

		public String GetName()
		{
			return nickName ?? name;
		}

		public bool Equals( PlayerData other )
		{
			return userId == other.userId;
		}

		public int CompareTo( PlayerData other )
		{
			if( team == null || other.team == null )
			{
				return userId.CompareTo( other.userId );
			}

			return team.score.CompareTo( other.team.score );
		}
	}
}
