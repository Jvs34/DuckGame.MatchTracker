using System;
using System.Collections.Generic;

namespace MatchTracker
{
	//hats are used to define teams in duck game, so we kinda do need to track them
	public class TeamData : IPlayersList
	{
		public bool HasHat { get; set; }
		public string HatName { get; set; }
		public bool IsCustomHat { get; set; }
		public List<string> Players { get; set; } = new List<string>();
		public int Score { get; set; }

		public override string ToString()
		{
			return $"{HatName} +{Score} " + string.Join( "," , Players );
		}
	}
}