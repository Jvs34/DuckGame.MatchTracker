namespace MatchShared.Interfaces;

public interface IDatabaseEntry
{
	/// <summary>
	/// Point this to another variable to make that the index
	/// </summary>
	string DatabaseIndex { get; }
}
