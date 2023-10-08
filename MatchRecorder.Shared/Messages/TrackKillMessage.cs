using MatchShared.DataClasses;

namespace MatchRecorder.Shared.Messages;

public class TrackKillMessage : BaseMessage
{
	public override string MessageType { get; set; } = nameof( TrackKillMessage );
	public KillData KillData { get; set; }
}
