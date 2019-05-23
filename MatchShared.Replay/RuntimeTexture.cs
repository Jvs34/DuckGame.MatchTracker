using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

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
        public byte[] Data;
    }
}