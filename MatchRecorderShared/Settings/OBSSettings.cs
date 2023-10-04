namespace MatchTracker;

public class OBSSettings
{
	public string WebSocketUri { get; set; }
	public string WebSocketPassword { get; set; }

	/// <summary>
	/// The Scene collection to set OBS to, leave empty if not desired
	/// </summary>
	public string SceneCollectionName { get; set; }

	/// <summary>
	/// The profile to set OBS to, leave empty if not desired
	/// </summary>
	public string ProfileName { get; set; }
}
