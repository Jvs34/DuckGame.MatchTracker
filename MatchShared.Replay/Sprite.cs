using ProtoBuf;
using System;
using System.Collections.Generic;

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

		public bool Equals( Sprite sprite )
		{
			return Texture == sprite.Texture &&
				   Material == sprite.Material &&
				   Center.Equals( sprite.Center ) &&
				   EqualityComparer<Rectangle>.Default.Equals( TexCoords , sprite.TexCoords ) &&
				   EqualityComparer<int?>.Default.Equals( RuntimeTextureIndex , sprite.RuntimeTextureIndex ) &&
				   EqualityComparer<object>.Default.Equals( TextureObject , sprite.TextureObject );
		}

		public override bool Equals( object obj )
		{
			if( !( obj is Sprite ) )
			{
				return false;
			}

			var sprite = (Sprite) obj;
			return Texture == sprite.Texture &&
				   Material == sprite.Material &&
				   Center.Equals( sprite.Center ) &&
				   EqualityComparer<Rectangle>.Default.Equals( TexCoords , sprite.TexCoords ) &&
				   EqualityComparer<int?>.Default.Equals( RuntimeTextureIndex , sprite.RuntimeTextureIndex ) &&
				   EqualityComparer<object>.Default.Equals( TextureObject , sprite.TextureObject );
		}

		public override int GetHashCode()
		{
			var hashCode = 311847513;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode( Texture );
			hashCode = hashCode * -1521134295 + Material.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Center );
			hashCode = hashCode * -1521134295 + EqualityComparer<Rectangle>.Default.GetHashCode( TexCoords );
			hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode( RuntimeTextureIndex );
			hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode( TextureObject );
			return hashCode;
		}
	}
}
