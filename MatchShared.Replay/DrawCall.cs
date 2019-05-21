using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker.Replay
{
	[ProtoContract]
	public struct DrawCall : IEquatable<DrawCall>
	{
		/// <summary>
		/// Things that change a lot
		/// </summary>
		[ProtoContract]
		public struct Properties
		{
			[ProtoMember( 1 )]
			public Vec2 Position;

			[ProtoMember( 2 )]
			public float? Angle;

			[ProtoMember( 3 )]
			public Vec2? Scale;
		}

		[ProtoMember( 1 )]
		public int EntityIndex;

		[ProtoMember( 2 )]
		public int SpriteIndex;

		[ProtoMember( 4 )]
		public double Depth;

		[ProtoMember( 5 )]
		public Color Color;

		[ProtoMember( 6 )]
		public bool FlipVertically;

		[ProtoMember( 7 )]
		public bool FlipHorizontally;

		public override bool Equals( object obj )
		{
			return obj is DrawCall && Equals( (DrawCall) obj );
		}

		public bool Equals( DrawCall other )
		{
			return EntityIndex == other.EntityIndex &&
				   SpriteIndex == other.SpriteIndex &&
				   Depth == other.Depth &&
				   Color.Equals( other.Color ) &&
				   FlipVertically == other.FlipVertically &&
				   FlipHorizontally == other.FlipHorizontally;
		}

		public override int GetHashCode()
		{
			var hashCode = 1025059079;
			hashCode = hashCode * -1521134295 + EntityIndex.GetHashCode();
			hashCode = hashCode * -1521134295 + SpriteIndex.GetHashCode();
			hashCode = hashCode * -1521134295 + Depth.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode( Color );
			hashCode = hashCode * -1521134295 + FlipVertically.GetHashCode();
			hashCode = hashCode * -1521134295 + FlipHorizontally.GetHashCode();
			return hashCode;
		}
	}
}