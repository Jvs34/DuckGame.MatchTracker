using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface ILevelList
	{
		DatabaseEntries<LevelData> Levels { get; set; }
	}
}
