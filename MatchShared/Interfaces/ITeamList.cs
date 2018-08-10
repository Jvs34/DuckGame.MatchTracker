using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface ITeamsList
	{
		List<TeamData> teams { get; set; }
	}
}