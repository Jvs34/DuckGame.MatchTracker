using System.Collections.Generic;

namespace MatchTracker;

/// <summary>
/// A level in Duck Game, used for stat tracking
/// </summary>
public class LevelData : ITagsList, IDatabaseEntry, ILevelName
{
	/// <summary>
	/// The level's guid
	/// </summary>
	public string LevelName { get; set; }

	/// <summary>
	/// The level's path on the filesystem
	/// </summary>
	public string FilePath { get; set; }

	/// <summary>
	/// Whether this is a map loaded from the workshop or sent over the network
	/// </summary>
	public bool IsCustomMap { get; set; }

	/// <summary>
	/// Is this an online enabled map?
	/// </summary>
	public bool IsOnlineMap { get; set; }

	/// <summary>
	/// Description of the level, if there's any
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Author of the level, if there's any
	/// </summary>
	public string Author { get; set; }

	public List<string> Tags { get; set; } = new List<string>();

	public string DatabaseIndex => LevelName;
}