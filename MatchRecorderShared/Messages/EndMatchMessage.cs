using MatchTracker;
using System;
using System.Collections.Generic;

namespace MatchRecorderShared.Messages
{
	public class EndMatchMessage : BaseMessage, IWinner, IEndTime
	{
		public override string MessageType { get; set; } = nameof( EndMatchMessage );
		public TeamData Winner { get; set; }
		public List<string> Players { get; set; }
		public List<TeamData> Teams { get; set; }
		public List<PlayerData> PlayersData { get; set; }
		public DateTime TimeEnded { get; set; }
		public bool Aborted { get; set; }

		public List<string> GetWinners() => Winner?.Players ?? new List<string>();
	}
}
