namespace MatchTracker;

/// <summary>
/// A Duck Game player, most of the time UserId represents the SteamID
/// </summary>
public class PlayerData : IDatabaseEntry
{
	public string Name { get; set; }

	/// <summary>
	/// A manually set Nickname for the player
	/// </summary>
	public string NickName { get; set; }

	/// <summary>
	/// In most cases this is the SteamID of the player, but not in offline mode
	/// </summary>
	public string UserId { get; set; }

	/// <summary>
	/// Discord user id, this is no longer used by Duck Game
	/// </summary>
	public ulong DiscordId { get; set; }
	public string DatabaseIndex => UserId;

	public string GetName( bool nickName = false ) => nickName ? NickName ?? Name : Name;
	public override string ToString() => $"{GetName()}:{UserId}";
}