﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using ProtoBuf.Meta;

namespace MatchTracker.Replay
{
	/// <summary>
	/// Represents an actual recording of the match, along with all the frames
	/// and references to whatever textures and other stuff were used
	/// </summary>
	public class ReplayRecording : IStartEnd
	{
		public string Name { get; set; }
		public DateTime TimeEnded { get; set; }
		public DateTime TimeStarted { get; set; }
		public TimeSpan GetDuration() => TimeEnded.Subtract( TimeStarted );

		public List<string> Textures => new List<string>();
		public List<string> Materials => new List<string>();

		private int CurrentFrameIndex { get; set; }

		struct Frame
		{
			public TimeSpan Time;
			public Rectangle CameraMovement;
			public List<(Sprite, DrawCall, DrawCall.Properties)> DrawCalls;
		}

		List<Frame> Frames = new List<Frame>();

		public void StartFrame( TimeSpan time , MatchTracker.Rectangle cameraData )
		{
			var newFrame = new Frame
			{
				Time = time ,
				CameraMovement = cameraData ,
				DrawCalls = new List<(Sprite, DrawCall, DrawCall.Properties)>()
			};

			Frames.Add( newFrame );
		}

		public void AddDrawCall( string texture , Vec2 position , Rectangle sourceRectangle , Color color , float rotation , Vec2 spriteCenter , Vec2 scale , int effects , double depth , int entityIndex )
		{
			Frame currentFrame = Frames [CurrentFrameIndex];

			var sprite = new Sprite
			{
				Texture = texture ,
				// Material = ,
				Center = spriteCenter ,
				TexCoords = sourceRectangle
			};

			var drawCall = new DrawCall
			{
				EntityIndex = entityIndex ,
				SpriteIndex = -1 ,
				Depth = depth ,
				Color = color ,
				FlipHorizontally = effects == 1 ,
				FlipVertically = effects == 2
			};

			var drawCallProperties = new DrawCall.Properties
			{
				Position = position ,
				Angle = null ,
				Scale = null
			};

			if( rotation != 0f )
				drawCallProperties.Angle = rotation;

			if( scale.X != 1f || scale.Y != 1f )
				drawCallProperties.Scale = scale;

			currentFrame.DrawCalls.Add( (sprite, drawCall, drawCallProperties) );
		}

		public void EndFrame()
		{
			CurrentFrameIndex++;
		}


		public void Serialize( Stream stream )
		{
			var replay = new Replay
			{
				Name = Name ,
				TimeEnded = TimeEnded ,
				TimeStarted = TimeStarted ,
				Textures = Textures ,
				Materials = Materials
			};

			///
			/// all of it
			///
			var spriteCount = 0;
			var drawCallCount = 0;
			var spriteHash = new Dictionary<Sprite , int>();
			var drawCallHash = new Dictionary<DrawCall , int>();

			foreach( var runtimeFrame in Frames )
			{
				var frame = new MatchTracker.Replay.Frame
				{
					Time = runtimeFrame.Time ,
					CameraMovement = runtimeFrame.CameraMovement ,
				};

				foreach( var drawCall in runtimeFrame.DrawCalls )
				{
					int spriteIndex;
					int drawCallIndex;

					if( !spriteHash.TryGetValue( drawCall.Item1 , out spriteIndex ) )
					{
						spriteIndex = spriteCount;
						spriteHash.Add( drawCall.Item1 , spriteCount++ );
					}

					var drawCall_Copy = drawCall.Item2;
					drawCall_Copy.SpriteIndex = spriteIndex;

					if( !drawCallHash.TryGetValue( drawCall_Copy , out drawCallIndex ) )
					{
						drawCallIndex = drawCallCount;
						drawCallHash.Add( drawCall_Copy , drawCallCount++ );
					}

					frame.DrawCallIndices.Add( drawCallIndex );
					frame.DrawCallProperties.Add( drawCall.Item3 );
				}

				replay.Frames.Add( frame );
			}

			var spriteArray = new Sprite [spriteCount];

			foreach( var spriteEntry in spriteHash )
			{
				spriteArray [spriteEntry.Value] = spriteEntry.Key;
			}

			replay.Sprites = spriteArray.ToList();

			var drawCallArray = new DrawCall [drawCallCount];

			foreach( var drawCallEntry in drawCallHash )
			{
				drawCallArray [drawCallEntry.Value] = drawCallEntry.Key;
			}

			replay.DrawCalls = drawCallArray.ToList();

			var drawcalls_all = drawCallArray.GroupBy( x => x.EntityIndex ).OrderBy( x => x.Count() );

			Serializer.Serialize( stream , replay );
		}

		public static Replay Unserialize( Stream stream )
		{
			return Serializer.Deserialize<Replay>( stream );
		}

		public static void InitProtoBuf()
		{
			//TODO: I am literally mixmatching attributes and manually adding stuff to protobuf, I need to fix this at some point
			RuntimeTypeModel.Default.Add( typeof( Vec2 ) , false ).Add( "X" , "Y" );
			RuntimeTypeModel.Default.Add( typeof( Color ) , false ).Add( "R" , "G" , "B" , "A" );
			RuntimeTypeModel.Default.Add( typeof( Rectangle ) , false ).Add( "Position" , "Width" , "Height" );
		}

		public static string GetProto()
		{
			return new StringBuilder()
				//.AppendLine( Serializer.GetProto<Vec2>() )
				//.AppendLine( Serializer.GetProto<Color>() )
				//.AppendLine( Serializer.GetProto<Rectangle>() )
				.AppendLine( Serializer.GetProto<Replay>() )
				//.AppendLine( Serializer.GetProto<MatchTracker.Replay.Frame>() )
				//.AppendLine( Serializer.GetProto<DrawCall.Properties>() )
				//.AppendLine( Serializer.GetProto<DrawCall>() )
				//.AppendLine( Serializer.GetProto<Sprite>() )
				.ToString();
		}
	}
}
