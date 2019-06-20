using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public interface IMatchesList
	{
		DatabaseEntries<MatchData> Matches { get; set; }
	}
}