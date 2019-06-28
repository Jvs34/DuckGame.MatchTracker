using ProtoBuf;

namespace MatchTracker.Replay
{
	[ProtoContract]
	public struct RuntimeTexture
	{
		[ProtoMember( 1 )]
		public int Width;

		[ProtoMember( 2 )]
		public int Height;

		[ProtoMember( 3 )]
		public byte [] Data;
	}
}