using ProtoBuf;
using System.Collections.Generic;

namespace MatchTracker.Replay
{
	[ProtoContract]
	public struct StaticDrawCall
	{
		[ProtoMember( 1 )]
		public List<int> DrawCallIndices;

		[ProtoMember( 2 )]
		public List<DrawCall.Properties> DrawCallProperties;
	}
}