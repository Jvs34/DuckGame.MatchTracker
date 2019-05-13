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
	}
}
