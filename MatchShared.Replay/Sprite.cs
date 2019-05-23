using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker.Replay
{
	[ProtoContract]
	public struct Sprite : IEquatable<Sprite>
	{
		[ProtoMember( 1 )]
		public string Texture;

		[ProtoMember( 2 )]
		public uint Material;

		[ProtoMember( 3 )]
		public Vec2 Center;

		[ProtoMember( 5 )]
		public Rectangle TexCoords;

        [ProtoMember( 6 )]
        public int? RuntimeTextureIndex;
        public object TextureObject;

		public override bool Equals( object obj )
		{
			return obj is Sprite && Equals( (Sprite) obj );
		}

		public bool Equals( Sprite other )
		{
			return Texture == other.Texture &&
				   Material == other.Material &&
				   Center.Equals( other.Center ) &&
				   EqualityComparer<Rectangle>.Default.Equals( TexCoords , other.TexCoords );
		}

		public override int GetHashCode()
		{
			var hashCode = -933968943;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode( Texture );
			hashCode = hashCode * -1521134295 + Material.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Center );
			hashCode = hashCode * -1521134295 + EqualityComparer<Rectangle>.Default.GetHashCode( TexCoords );
			return hashCode;
		}
	}
}
