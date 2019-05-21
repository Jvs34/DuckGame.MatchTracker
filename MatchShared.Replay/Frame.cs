using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker.Replay
{
	/// <summary>
	/// Represents a drawn frame from duck game and contains
	/// all the draw calls that were done in this frame
	/// </summary>
	[ProtoContract]
	public class Frame
	{
		[ProtoMember( 1 )]
		public TimeSpan Time { get; set; }

		[ProtoMember( 2 )]
		public Rectangle CameraMovement { get; set; }

		[ProtoMember( 4 )]
		public List<int> DrawCallIndices = new List<int>();

		[ProtoMember( 5 )]
		public List<DrawCall.Properties> DrawCallProperties = new List<DrawCall.Properties>();
	}
}
