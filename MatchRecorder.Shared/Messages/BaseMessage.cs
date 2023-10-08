namespace MatchRecorder.Shared.Messages;

public abstract class BaseMessage
{
	public virtual string MessageType { get; set; } = nameof( BaseMessage );
}
