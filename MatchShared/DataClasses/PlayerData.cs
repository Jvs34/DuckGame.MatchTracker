using System;

namespace MatchTracker
{
	//duck game networked profiles aren't all that networked really, you only get the name and id
	public class PlayerData : IEquatable<PlayerData>, IComparable<PlayerData>
	{
		public ulong DiscordId { get; set; }

		public string Name { get; set; }

		//custom nickname for the player, this will be set manually on another json
		public string NickName { get; set; }

		//usually the steamid, if this is a localplayer it will be PROFILE1/2/3/4 whatever
		public string UserId { get; set; }

		public int CompareTo( PlayerData other )
		{
			return UserId.CompareTo( other.UserId );
		}

		public bool Equals( PlayerData other )
		{
			return UserId == other.UserId;
		}

		public string GetName()
		{
			return NickName ?? Name;
		}
	}
}