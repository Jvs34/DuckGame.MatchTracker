using System.Collections.Generic;

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
