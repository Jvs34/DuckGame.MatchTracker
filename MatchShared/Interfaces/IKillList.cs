using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public interface IKillList
	{
		public List<KillData> Kills { get; set; }
	}
}
