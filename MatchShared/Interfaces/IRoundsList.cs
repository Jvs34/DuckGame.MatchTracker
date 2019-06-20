using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public interface IRoundsList
	{
		DatabaseEntries<RoundData> Rounds { get; set; }
	}
}