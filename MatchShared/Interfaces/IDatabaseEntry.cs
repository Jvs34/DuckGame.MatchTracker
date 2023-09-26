namespace MatchTracker
{
	public interface IDatabaseEntry
	{
		/// <summary>
		/// Point this to another variable to make that the index
		/// </summary>
		string DatabaseIndex { get; }
	}
}
