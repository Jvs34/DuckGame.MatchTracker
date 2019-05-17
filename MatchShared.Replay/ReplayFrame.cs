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
	public class ReplayFrame
	{

		[ProtoMember( 1 )]
		public TimeSpan Time { get; set; }

		//public List<ReplayDrawnItem> DrawCalls { get; set; } = new List<ReplayDrawnItem>();

		//TODO: turn the list into a hashset
		[ProtoMember( 2 )]
		public Dictionary<int , HashSet<ReplayDrawnItem>> DrawCalls { get; set; } = new Dictionary<int , HashSet<ReplayDrawnItem>>();

		[ProtoMember( 3 )]
		public Rectangle CameraMovement { get; set; }
	}
}
