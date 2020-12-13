using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class EndMatchMessage : BaseMessage, IWinner
	{
		public override string MessageType { get; set; } = nameof( EndMatchMessage );
		public TeamData Winner { get; set; }
		public List<string> Players { get; set; }
		public List<TeamData> Teams { get; set; }

		public List<string> GetWinners() => Winner?.Players ?? new List<string>();
	}
}
