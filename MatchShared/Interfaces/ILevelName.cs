using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface ILevelName
	{
		/// <summary>
		/// Name of the level, this is not a filepath, also this might be "RANDOM" for random levels
		/// </summary>
		string LevelName { get; set; }
	}
}
