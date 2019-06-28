using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MatchTracker.Replay.DeltaFormat
{
	public class ReplayRecorder
	{
		private string _name;
		private bool _inFrame;
		private int _frameNum;

		private Stack<object> _currentObjectStack = new Stack<object>();
		private Stack<List<int>> _currentDrawCallsStack = new Stack<List<int>>();

		private object _currentObject;
		private List<int> _currentDrawCalls = new List<int>();

		private Dictionary<Sprite , int> _sprites = new Dictionary<Sprite , int>();
		private Dictionary<DrawCall , int> _drawCalls = new Dictionary<DrawCall , int>();

		private Dictionary<object , List<(int duration, List<int> drawCalls)>> _entityDrawCalls = new Dictionary<object , List<(int duration, List<int> drawCalls)>>();

		public ReplayRecorder( string name )
		{
			_name = name;
		}

		public void StartFrame()
		{
			_inFrame = true;
		}

		public void DrawTexture( string textureName , object textureObj , Vec2 position , Rectangle texCoords , Color color , float rotation , Vec2 spriteCenter , Vec2 scale , int effects , double depth )
		{
			if( !_inFrame )
			{
				return;
			}

			// TODO
			if( _currentObject == null )
			{
				return;
			}

			int drawCallIndex;
			{
				var sprite = new Sprite
				{
					Texture = textureName ,
					Center = spriteCenter ,
					TexCoords = texCoords
				};

				var drawCall = new DrawCall
				{
					SpriteIndex = GetSpriteIndex( sprite ) ,
					Position = position ,
					Rotation = rotation ,
					Scale = scale ,
					Depth = depth ,
					Color = color
				};

				drawCallIndex = GetDrawCallIndex( drawCall );
			}

			_currentDrawCalls.Add( drawCallIndex );
		}

		public void EndFrame()
		{
			_inFrame = false;
			_frameNum++;
		}

		public void OnStartDrawingObject( object obj )
		{
			_currentObjectStack.Push( _currentObject );
			_currentDrawCallsStack.Push( _currentDrawCalls );

			_currentObject = obj;
			_currentDrawCalls = new List<int>();
		}

		public void OnFinishDrawingObject( object obj )
		{
			if( !_inFrame || _currentObject != obj )
			{
				throw new Exception();
			}

			List<(int duration, List<int> drawCalls)> drawCalls;

			if( !_entityDrawCalls.TryGetValue( _currentObject , out drawCalls ) )
			{
				drawCalls = new List<(int duration, List<int> drawCalls)>();
				_entityDrawCalls.Add( _currentObject , drawCalls );

				if( _frameNum > 0 )
				{
					drawCalls.Add( (_frameNum, null) );
				}
			}

			var idx = drawCalls.Count - 1;
			if( drawCalls.Count == 0 || drawCalls [idx].drawCalls == null || !drawCalls [idx].drawCalls.SequenceEqual( _currentDrawCalls ) )
			{
				drawCalls.Add( (1, _currentDrawCalls.ToList()) ); // Copy here so that we don't give away our reference
			}
			else
			{
				drawCalls [idx] = (drawCalls [idx].duration + 1, drawCalls [idx].drawCalls);
			}

			_currentObject = _currentObjectStack.Pop();
			_currentDrawCalls = _currentDrawCallsStack.Pop();
		}

		public void Serialize( Stream outStream )
		{
			Replay replay = new Replay
			{
				Name = _name ,
				DrawCalls = new List<DrawCall>() ,
				Frames = new List<Frame>() ,
				Entities = new List<Entity>()
			};
		}

		private int GetSpriteIndex( Sprite sprite )
		{
			int index;
			if( !_sprites.TryGetValue( sprite , out index ) )
			{
				index = _sprites.Count;
				_sprites.Add( sprite , index );
			}

			return index;
		}

		private int GetDrawCallIndex( DrawCall drawCall )
		{
			int index;
			if( !_drawCalls.TryGetValue( drawCall , out index ) )
			{
				index = _drawCalls.Count;
				_drawCalls.Add( drawCall , index );
			}

			return index;
		}
	}

	[ProtoContract]
	public struct Replay
	{
		//
		// Data that is written to our output
		//
		[ProtoMember( 1 )]
		internal string Name;

		[ProtoMember( 2 )]
		internal List<DrawCall> DrawCalls;

		[ProtoMember( 3 )]
		internal List<Frame> Frames;

		[ProtoMember( 2 )]
		internal List<Entity> Entities;
	}

	[ProtoContract]
	struct Frame
	{
		[ProtoMember( 1 )]
		public List<int> DrawCallIndices;
	}

	[ProtoContract]
	struct DrawCall : IEquatable<DrawCall>
	{
		[ProtoMember( 1 )]
		public int SpriteIndex;

		[ProtoMember( 1 )]
		public Vec2 Position;

		[ProtoMember( 2 )]
		public float Rotation;

		[ProtoMember( 3 )]
		public Vec2 Scale;

		[ProtoMember( 4 )]
		public double Depth;

		[ProtoMember( 1 )]
		public Color Color;

		public override bool Equals( object obj )
		{
			return obj is DrawCall && Equals( (DrawCall) obj );
		}

		public bool Equals( DrawCall other )
		{
			return SpriteIndex == other.SpriteIndex &&
				   Position.Equals( other.Position ) &&
				   Rotation == other.Rotation &&
				   Scale.Equals( other.Scale ) &&
				   Depth == other.Depth &&
				   Color.Equals( other.Color );
		}

		public override int GetHashCode()
		{
			var hashCode = -1831673700;
			hashCode = hashCode * -1521134295 + SpriteIndex.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Position );
			hashCode = hashCode * -1521134295 + Rotation.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Scale );
			hashCode = hashCode * -1521134295 + Depth.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode( Color );
			return hashCode;
		}
	}

	[ProtoContract]
	struct DrawCall_RLE
	{
		[ProtoMember( 1 )]
		public int Duration;

		[ProtoMember( 2 )]
		public List<int> DrawCallIndices;
	}

	[ProtoContract]
	struct Entity
	{
		[ProtoMember( 1 )]
		List<DrawCall_RLE> DrawCallData;
	}

	[ProtoContract]
	public struct Sprite : IEquatable<Sprite>
	{
		[ProtoMember( 1 )]
		public string Texture;

		[ProtoMember( 2 )]
		public Vec2 Center;

		[ProtoMember( 3 )]
		public Rectangle TexCoords;

		public override bool Equals( object obj )
		{
			return obj is Sprite && Equals( (Sprite) obj );
		}

		public bool Equals( Sprite other )
		{
			return Texture == other.Texture &&
				   Center.Equals( other.Center ) &&
				   EqualityComparer<Rectangle>.Default.Equals( TexCoords , other.TexCoords );
		}

		public override int GetHashCode()
		{
			var hashCode = 1373877432;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode( Texture );
			hashCode = hashCode * -1521134295 + EqualityComparer<Vec2>.Default.GetHashCode( Center );
			hashCode = hashCode * -1521134295 + EqualityComparer<Rectangle>.Default.GetHashCode( TexCoords );
			return hashCode;
		}
	}
}
