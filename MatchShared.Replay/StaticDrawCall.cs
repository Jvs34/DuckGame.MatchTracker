using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

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