using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public struct Color : IEquatable<Color>
	{
		public byte R { get; set; }
		public byte G { get; set; }
		public byte B { get; set; }
		public byte A { get; set; }

		public bool Equals( Color other )
		{
			return other.R.Equals( R )
				&& other.G.Equals( G )
				&& other.B.Equals( B )
				&& other.A.Equals( A );
		}

		public override int GetHashCode()
		{
			var hashCode = 1960784236;
			hashCode = hashCode * -1521134295 + R.GetHashCode();
			hashCode = hashCode * -1521134295 + G.GetHashCode();
			hashCode = hashCode * -1521134295 + B.GetHashCode();
			hashCode = hashCode * -1521134295 + A.GetHashCode();
			return hashCode;
		}

		public override string ToString()
		{
			return $"Color R: {R} G: {G} B: {B} A: {A}";
		}
	}
}
