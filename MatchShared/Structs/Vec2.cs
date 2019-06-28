using System;

namespace MatchTracker
{
	public struct Vec2 : IEquatable<Vec2>
	{
		public float X { get; set; }
		public float Y { get; set; }

		public bool Equals( Vec2 other )
		{
			return other.X.Equals( X ) && other.Y.Equals( Y );
		}

		public override int GetHashCode()
		{
			var hashCode = 1861411795;
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			return hashCode;
		}

		public override string ToString()
		{
			return $"Vec2 X: {X} Y: {Y}";
		}
	}
}
