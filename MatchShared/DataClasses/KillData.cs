using System;

namespace MatchTracker;

/// <summary>
/// A kill in a round, not saved directly in the database but as part of <see cref="RoundData.KillsList"/>
/// </summary>
public class KillData
{

	/// <summary>
	/// Killer, could be the victim themselves or even null
	/// </summary>
	public TeamData Killer { get; set; }

	/// <summary>
	/// The teamdata for the victim, will never be null
	/// </summary>
	public TeamData Victim { get; set; }

	public DateTime TimeOccured { get; set; }

	/// <summary>
	/// The DestroyType as as string, eg: DTFall, DTShot etc
	/// </summary>
	public string DeathTypeClassName { get; set; }

	/// <summary>
	/// The actual weapon or object used to kill the duck, eg: QuadLaser
	/// </summary>
	public string ObjectClassName { get; set; }
}
