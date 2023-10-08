using MatchShared.Interfaces;

namespace MatchShared.DataClasses;

/// <summary>
/// A type of death in Duck Game, eg: DTFall for fall damage
/// </summary>
public class DestroyTypeData : IDatabaseEntry
{
	public string DatabaseIndex => ClassName;

	/// <summary>
	/// The classname of the death type eg: DTFall
	/// </summary>
	public string ClassName { get; set; }

	/// <summary>
	/// The fancy name of the death type eg: Fall Damage
	/// </summary>
	public string Name { get; set; }
}
