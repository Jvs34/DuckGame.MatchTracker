namespace MatchShared.Interfaces;

public interface ILevelName
{
	/// <summary>
	/// Name of the level, this is not a filepath, also this might be "RANDOM" for random levels
	/// </summary>
	string LevelName { get; set; }
}
