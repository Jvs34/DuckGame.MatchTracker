namespace MatchTracker;

/// <summary>
/// A Duck Game object that can cause a death, like a gun or even a crate
/// </summary>
public class ObjectData : IDatabaseEntry
{
	public string DatabaseIndex => ClassName;

	/// <summary>
	/// The classname of the object, eg Warpgun
	/// </summary>
	public string ClassName { get; set; }

	/// <summary>
	/// The editor name of the object, eg WAGNUS
	/// </summary>
	public string EditorName { get; set; }

	/// <summary>
	/// The editor description of the object
	/// </summary>
	public string EditorDescription { get; set; }

	/// <summary>
	/// The editor tooltip of the object
	/// </summary>
	public string EditorTooltip { get; set; }

	/// <summary>
	/// The bio description of the object
	/// </summary>
	public string BioDescription { get; set; }
}