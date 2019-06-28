using Newtonsoft.Json;

namespace MatchTracker
{
	public interface IDatabaseEntry
	{
		[JsonIgnore]
		string DatabaseIndex { get; }
	}
}
