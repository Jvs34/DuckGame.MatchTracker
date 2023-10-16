namespace MatchRecorder.Shared.Structs;

public struct RGBAColor
{
	public byte R;
	public byte G;
	public byte B;
	public byte A;

	public override readonly string ToString() => $"{R} {G} {B} {A}";
}
