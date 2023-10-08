using MatchShared.DataClasses;
using System.Collections.Generic;

namespace MatchShared.Interfaces;

public interface IWinner : IPlayersList, ITeamsList
{
	/// <summary>
	/// The winner Team, might contain multiple players
	/// </summary>
	TeamData Winner { get; set; }

	/// <summary>
	/// The winning players, contains multiple only in team mode
	/// </summary>
	/// <returns>Player database indexes</returns>
	List<string> GetWinners();
}