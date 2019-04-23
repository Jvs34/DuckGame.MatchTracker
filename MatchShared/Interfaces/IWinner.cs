using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//should this inherit IPlayerList? a winner is always accompanied by a player list
	//this'll probably be more useful once we get interface traits in c# 8.0
	public interface IWinner : IPlayersList, ITeamsList
	{
		TeamData Winner { get; set; }

		string GetWinnerName();

		List<PlayerData> GetWinners();
	}
}