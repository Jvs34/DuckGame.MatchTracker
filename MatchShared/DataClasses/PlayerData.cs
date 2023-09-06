using System;

namespace MatchTracker
{
	//duck game networked profiles aren't all that networked really, you only get the name and id
	public class PlayerData : IDatabaseEntry
	{
		public string Name { get; set; }
		public string NickName { get; set; }
		public string UserId { get; set; }
		public ulong DiscordId { get; set; }
		public string DatabaseIndex => UserId;

		public string GetName() => NickName ?? Name;
		public override string ToString() => GetName();
	}
}