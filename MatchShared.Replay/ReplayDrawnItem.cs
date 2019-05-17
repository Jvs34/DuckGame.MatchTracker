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
	public class ReplayDrawnItem : IEquatable<ReplayDrawnItem>, ICloneable
	{
		[ProtoMember( 1 )]
		public int EntityIndex { get; set; }

		[ProtoMember( 2 )]
		public string Texture { get; set; }

		[ProtoMember( 3 )]
		public uint Material { get; set; }

		[ProtoMember( 4 )]
		public Vec2 Position { get; set; }

		[ProtoMember( 5 )]
		public Vec2 Center { get; set; }

		[ProtoMember( 6 )]
		public Vec2 Scale { get; set; }

		[ProtoMember( 7 )]
		public Rectangle TexCoords { get; set; }

		[ProtoMember( 8 )]
		public float Angle { get; set; }

		/// <summary>
		/// The depth that this draw call wants to be drawn at
		/// Dunno why this should be a double but duck game uses it like this
		/// </summary>
		[ProtoMember( 9 )]
		public double Depth { get; set; }

		[ProtoMember( 10 )]
		public Color Color { get; set; }

		[ProtoMember( 11 )]
		public bool FlipVertically { get; set; }

		[ProtoMember( 12 )]
		public bool FlipHorizontally { get; set; }

		/// <summary>
		/// <para>For how many consequent frames this draw call is repeated, defaults to 0</para>
		/// <para>Only set after ReplayRecorder.TrimDrawCalls is finished</para>
		/// </summary>
		[ProtoMember( 13 )]
		public int Repetitions { get; set; }

		public object Clone()
		{
			return new ReplayDrawnItem()
			{
				Angle = Angle ,
				Center = Center ,
				Color = Color ,
				Depth = Depth ,
				EntityIndex = EntityIndex ,
				Material = Material ,
				Position = Position ,
				Scale = Scale ,
				TexCoords = TexCoords ,
				Texture = Texture ,
				FlipHorizontally = FlipHorizontally ,
				FlipVertically = FlipVertically ,
			};
		}

		public bool Equals( ReplayDrawnItem other )
		{
			return other.EntityIndex == EntityIndex
				&& other.Angle.Equals( Angle )
				&& other.Center.Equals( Center )
				&& other.Color.Equals( Color )
				&& other.Depth.Equals( Depth )
				&& other.Position.Equals( Position )
				&& other.Scale.Equals( Scale )
				&& other.Texture.Equals( Texture )
				&& other.TexCoords.Equals( TexCoords )
				&& other.FlipVertically.Equals( FlipVertically )
				&& other.FlipHorizontally.Equals( FlipHorizontally );
		}

		public override int GetHashCode()
		{
			var hashCode = -773154337;
			hashCode = hashCode * -1521134295 + EntityIndex.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode( Texture );
			hashCode = hashCode * -1521134295 + Material.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Position );
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Center );
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Scale );
			hashCode = hashCode * -1521134295 + EqualityComparer<Rectangle>.Default.GetHashCode( TexCoords );
			hashCode = hashCode * -1521134295 + Angle.GetHashCode();
			hashCode = hashCode * -1521134295 + Depth.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode( Color );
			hashCode = hashCode * -1521134295 + FlipVertically.GetHashCode();
			hashCode = hashCode * -1521134295 + FlipHorizontally.GetHashCode();
			return hashCode;
		}
	}
}
