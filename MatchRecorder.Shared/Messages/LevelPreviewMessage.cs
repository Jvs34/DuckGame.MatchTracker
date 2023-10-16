using MatchRecorder.Shared.Structs;

namespace MatchRecorder.Shared.Messages;

public class LevelPreviewMessage : BaseMessage
{
	public override string MessageType { get; set; } = nameof( LevelPreviewMessage );
	public string LevelName { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public RGBAColor[] PixelArray { get; set; }
}
