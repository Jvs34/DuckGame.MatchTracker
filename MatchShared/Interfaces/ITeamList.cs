using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface ITeamsList
	{
		List<TeamData> Teams { get; set; }
	}
}