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

		public override int GetHashCode()
		{
			var hashCode = -1214283325;
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Position );
			hashCode = hashCode * -1521134295 + Width.GetHashCode();
			hashCode = hashCode * -1521134295 + Height.GetHashCode();
			return hashCode;
		}

		public override string ToString()
		{
			return $"Rect P: {Position} W: {Width} H: {Height}";
		}
	}
}
