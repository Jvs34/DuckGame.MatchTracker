using MatchShared.DataClasses;
using MatchShared.Interfaces;
using System;
using System.Collections.Generic;

namespace MatchRecorder.Shared.Messages;

public class StartMatchMessage : BaseMessage, IPlayersList, ITeamsList, IStartTime
{
	public override string MessageType { get; set; } = nameof( StartMatchMessage );
	public List<string> Players { get; set; }
	public List<TeamData> Teams { get; set; }
	public List<PlayerData> PlayersData { get; set; }
	public DateTime TimeStarted { get; set; }
}
