using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker.Replay
{

	/// <summary>
	/// Represents an actual draw call
	/// </summary>
	[ProtoContract]
	public class Replay : IStartEnd
	{
		[ProtoMember( 1 )]
		public string Name { get; set; }

		[ProtoMember( 2 )]
		public DateTime TimeEnded { get; set; }

		[ProtoMember( 3 )]
		public DateTime TimeStarted { get; set; }

		[ProtoMember( 4 )]
		public List<Sprite> Sprites = new List<Sprite>();

		[ProtoMember( 5 )]
		public List<DrawCall> DrawCalls = new List<DrawCall>();

		[ProtoMember( 6 )]
		public List<Frame> Frames = new List<Frame>();

        [ProtoMember( 7 )]
        public List<StaticDrawCall> StaticDrawCalls = new List<StaticDrawCall>();

        [ProtoMember( 10 )]
        public List<RuntimeTexture> RuntimeTextures = new List<RuntimeTexture>();

		/// <summary>
		/// The list of textures that were used in this recording,
		/// so a replay viewer can cache them right away
		/// Also these will be referenced directly from a ReplayDrawnItem
		/// </summary>
		[ProtoMember( 8 )]
		public List<string> Textures { get; set; } = new List<string>();

		[ProtoMember( 9 )]
		public List<string> Materials { get; set; } = new List<string>();


		public TimeSpan GetDuration() => TimeEnded.Subtract( TimeStarted );
	}
}