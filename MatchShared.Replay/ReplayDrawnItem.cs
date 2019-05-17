using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker.Replay
{

	/// <summary>
	/// Represents an actual draw call
	/// </summary>
	public class ReplayDrawnItem : IEquatable<ReplayDrawnItem>, ICloneable
	{
		public int EntityIndex { get; set; }
		public string Texture { get; set; }
		public uint Material { get; set; }
		public Vec2 Position { get; set; }
		public Vec2 Center { get; set; }
		public Vec2 Scale { get; set; }
		public Rectangle TexCoords { get; set; }
		public float Angle { get; set; }

		/// <summary>
		/// The depth that this draw call wants to be drawn at
		/// Dunno why this should be a double but duck game uses it like this
		/// </summary>
		public double Depth { get; set; }

		public Color Color { get; set; }

		public bool FlipVertically { get; set; }
		public bool FlipHorizontally { get; set; }

		/// <summary>
		/// <para>For how many consequent frames this draw call is repeated, defaults to 0</para>
		/// <para>Only set after ReplayRecorder.TrimDrawCalls is finished</para>
		/// </summary>
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
	}
}
