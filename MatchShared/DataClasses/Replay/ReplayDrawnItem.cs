using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{

	/// <summary>
	/// Represents an actual draw call
	/// </summary>
	public class ReplayDrawnItem //TODO: Iequatable and whatever else
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
	}
}
