using System.Collections.Generic;

namespace MatchTracker;

/// <summary>
/// A Duck Game Team, defined by a hat or lack of, even hatless players have teams
/// </summary>
public class TeamData : IPlayersList
{
	public bool HasHat { get; set; }
	public string HatName { get; set; }
	public bool IsCustomHat { get; set; }
	public List<string> Players { get; set; } = new List<string>();
	public int Score { get; set; }

	public override string ToString()
	{
		return $"{HatName} +{Score} " + string.Join( "," , Players );
	}
}