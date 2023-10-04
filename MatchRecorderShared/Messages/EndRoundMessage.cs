using MatchTracker;
using System;
using System.Collections.Generic;

namespace MatchRecorderShared.Messages;

public class EndRoundMessage : BaseMessage, IWinner, IEndTime
{
	public override string MessageType { get; set; } = nameof( EndRoundMessage );
	public TeamData Winner { get; set; }
	public List<string> Players { get; set; }
	public List<TeamData> Teams { get; set; }
	public DateTime TimeEnded { get; set; }

	public List<string> GetWinners() => Winner?.Players ?? new List<string>();
}
