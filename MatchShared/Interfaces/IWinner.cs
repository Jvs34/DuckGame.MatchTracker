using System.Collections.Generic;

namespace MatchTracker
{
	public interface IWinner : IPlayersList, ITeamsList
	{
		TeamData Winner { get; set; }
		List<string> GetWinners();
	}
}