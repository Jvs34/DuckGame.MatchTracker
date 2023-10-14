using MatchShared.DataClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorder.Shared.Messages;

public class CollectLevelDataMessage : BaseMessage
{
	public override string MessageType { get; set; } = nameof( CollectLevelDataMessage );
	public List<LevelData> Levels { get; set; } = new List<LevelData>();
}
