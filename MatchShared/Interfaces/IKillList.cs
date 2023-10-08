using MatchShared.DataClasses;
using System.Collections.Generic;

namespace MatchShared.Interfaces;

public interface IKillList
{
	/// <summary>
	/// List of kills, not ordered
	/// </summary>
	public List<KillData> KillsList { get; set; }
}
