using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class StartRoundMessage : BaseMessage, ITeamsList, IPlayersList, ILevelName, IStartTime
	{
		public override string MessageType { get; set; } = nameof( StartRoundMessage );
		public string LevelName { get; set; }
		public List<TeamData> Teams { get; set; }
		public List<string> Players { get; set; }
		public DateTime TimeStarted { get; set; }
	}
}
