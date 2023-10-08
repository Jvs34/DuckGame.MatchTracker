using MatchShared.DataClasses;
using MatchShared.Interfaces;
using System;
using System.Collections.Generic;

namespace MatchRecorder.Shared.Messages;

public class StartRoundMessage : BaseMessage, ITeamsList, IPlayersList, ILevelName, IStartTime
{
	public override string MessageType { get; set; } = nameof( StartRoundMessage );
	public string LevelName { get; set; }
	public List<TeamData> Teams { get; set; }
	public List<string> Players { get; set; }
	public DateTime TimeStarted { get; set; }
}
