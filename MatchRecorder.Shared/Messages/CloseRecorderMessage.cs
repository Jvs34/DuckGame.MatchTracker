﻿namespace MatchRecorder.Shared.Messages;

public class CloseRecorderMessage : BaseMessage
{
	public override string MessageType { get; set; } = nameof( CloseRecorderMessage );
}
