using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public class Rectangle : IEquatable<Rectangle>
	{
		public Vec2 Position { get; set; }
		public float Width { get; set; }
		public float Height { get; set; }

		public bool Equals( Rectangle other )
		{
			return other.Position.Equals( Position )
				&& other.Width.Equals( Width )
				&& other.Height.Equals( Height );
		}
	}
}
