using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface IKillList
	{
		/// <summary>
		/// List of kills, not ordered
		/// </summary>
		public List<KillData> KillsList { get; set; }
	}
}
