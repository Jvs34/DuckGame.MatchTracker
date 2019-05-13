using System;
using System.Collections.Generic;
using System.Text;

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
	}
}
