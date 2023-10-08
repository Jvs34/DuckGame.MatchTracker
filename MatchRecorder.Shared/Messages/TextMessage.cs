using MatchRecorder.Shared.Enums;

namespace MatchRecorder.Shared.Messages;

public class TextMessage : BaseMessage
{
	public override string MessageType { get; set; } = nameof( TextMessage );
	public string Message { get; set; } = string.Empty;
	public TextMessagePosition MessagePosition { get; set; } = TextMessagePosition.TopLeft;
}
