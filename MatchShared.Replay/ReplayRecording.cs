using System;
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
	[ProtoContract]
	public class ReplayRecording : IStartEnd
	{
		[ProtoMember( 1 )]
		public string Name { get; set; }

		[ProtoMember( 2 )]
		public DateTime TimeEnded { get; set; }

		[ProtoMember( 3 )]
		public DateTime TimeStarted { get; set; }

		public TimeSpan GetDuration() => TimeEnded.Subtract( TimeStarted );

		private int CurrentFrame { get; set; }

		[ProtoMember( 4 )]
		public List<ReplayFrame> Frames { get; set; } = new List<ReplayFrame>();//TODO: change to a different list or array type?

		/// <summary>
		/// The list of textures that were used in this recording,
		/// so a replay viewer can cache them right away
		/// Also these will be referenced directly from a ReplayDrawnItem
		/// </summary>
		[ProtoMember( 5 )]
		public List<string> Textures { get; set; } = new List<string>();

		[ProtoMember( 6 )]
		public List<string> Materials { get; set; } = new List<string>();



		/// <summary>
		/// <para>
		/// This is a quite expensive function, trims all the draw calls that were unchanged throughout
		/// all the replay frames, to shave repeated data
		/// </para>
		/// <para>Use once before saving to file</para>
		/// </summary>
		public void TrimDrawCalls()
		{
			//for each frame, we need to find a draw call that has a duplicate item in the next frame, if it does, increment DrawCall.Repetitions, remove the item and keep scanning deeper
			for( int frameIndex = 0; frameIndex < Frames.Count; frameIndex++ )
			{
				var frame = Frames [frameIndex];

				//now go through all the draw calls per entity index
				foreach( var kv in frame.DrawCalls )
				{
					foreach( var drawCall in kv.Value )
					{
						//ok, now go through the next frames
						drawCall.Repetitions = GetAndRemoveDuplicates( drawCall , kv.Key , frameIndex + 1 );
					}
				}
			}
		}


		/// <summary>
		/// Searches deep into the drawcalls, stops when it can't find the requested 
		/// </summary>
		/// <param name="originalDrawcall">The original draw call to compare to</param>
		/// <param name="entityIndex">The index to use in the search</param>
		/// <param name="startAt">The index of the array to start at</param>
		/// <returns>The amount of duplicates that were removed</returns>
		private int GetAndRemoveDuplicates( ReplayDrawnItem originalDrawcall , int entityIndex , int startAt )
		{
			if( startAt >= Frames.Count )
			{
				return 0;
			}

			int duplicates = 0;

			for( int fIndex = startAt; fIndex < Frames.Count; fIndex++ )
			{
				//a frame has to both contain the same entityIndex of our draw call and the actual drawcall
				if( Frames [fIndex].DrawCalls.TryGetValue( entityIndex , out var drawCalls ) && drawCalls.Contains( originalDrawcall ) )
				{
					duplicates++;
					drawCalls.Remove( originalDrawcall );
				}
				else
				{
					break;
				}

			}


			return duplicates;
		}

		private void PopulateDuplicates( ReplayDrawnItem originalDrawcall , int entityIndex , int startAt , int duplicates )
		{
			if( startAt >= Frames.Count || duplicates <= 0 )
			{
				return;
			}

			for( int fIndex = startAt; fIndex < Frames.Count; fIndex++ )
			{
				//if we can index this draw call by the index, this means that at least the entity is valid
				if( Frames [fIndex].DrawCalls.TryGetValue( entityIndex , out var drawCalls ) )
				{
					if( drawCalls == null )
					{
						drawCalls = new HashSet<ReplayDrawnItem>();
						Frames [fIndex].DrawCalls [entityIndex] = drawCalls;
					}

					drawCalls.Add( (ReplayDrawnItem) originalDrawcall.Clone() );
					duplicates--;
				}
				else
				{
					break;
				}


				if( duplicates != 0 )
				{
					continue;
				}

				break;
			}
		}

		/// <summary>
		/// <para>
		/// The opposite of the above function, useful on the viewers side to not have to do keyframe interpolation
		/// bullshit like remembering which frames had repetitions and shit
		/// </para>
		/// <para>Use once before displaying with the viewer</para>
		/// </summary>
		public void DuplicateDrawCalls()
		{
			for( int frameIndex = 0; frameIndex < Frames.Count; frameIndex++ )
			{
				var frame = Frames [frameIndex];

				//now go through all the draw calls per entity index

				foreach( var kv in frame.DrawCalls )
				{
					foreach( var drawCall in kv.Value )
					{
						//ok, now go through the next frames
						PopulateDuplicates( drawCall , kv.Key , frameIndex + 1 , drawCall.Repetitions );
						drawCall.Repetitions = 0;
					}

				}


			}

		}

		public ReplayFrame StartFrame()
		{
			var replayFrame = new ReplayFrame();

			Frames.Add( replayFrame );

			return replayFrame;
		}

		public void AddDrawCall( string texture , Vec2 position , Rectangle sourceRectangle , Color color , float rotation , Vec2 spriteCenter , Vec2 scale , int effects , double depth , int entityIndex )
		{
			ReplayFrame replayFrame = Frames [CurrentFrame];

			//not very elegant but it works out

			if( !replayFrame.DrawCalls.TryGetValue( entityIndex , out HashSet<ReplayDrawnItem> drawCalls ) )
			{
				drawCalls = new HashSet<ReplayDrawnItem>();
				replayFrame.DrawCalls [entityIndex] = drawCalls;
			}


			drawCalls.Add( new ReplayDrawnItem()
			{
				EntityIndex = entityIndex ,
				Angle = rotation ,
				TexCoords = sourceRectangle ,
				Depth = depth ,
				Center = spriteCenter ,
				Position = position ,
				Scale = scale ,
				Color = color ,
				Texture = texture ,
				FlipHorizontally = effects == 1 ,
				FlipVertically = effects == 2 ,
				//TODO: materials along with their params
				//TODO: repetitions, although that should be added to the trim shit
			} );
		}

		public ReplayFrame EndFrame()
		{
			var replayFrame = Frames [CurrentFrame];
			CurrentFrame++;
			return replayFrame;
		}


		public static void Serialize( Stream stream , ReplayRecording recording )
		{
			Serializer.Serialize( stream , recording );
		}

		public static ReplayRecording Unserialize( Stream stream )
		{
			return Serializer.Deserialize<ReplayRecording>( stream );
		}

		public static void InitProtoBuf()
		{
			//TODO: I am literally mixmatching attributes and manually adding stuff to protobuf, I need to fix this at some point
			RuntimeTypeModel.Default.Add( typeof( Vec2 ) , false ).Add( "X" , "Y" );
			RuntimeTypeModel.Default.Add( typeof( Color ) , false ).Add( "R" , "G" , "B" , "A" );
			RuntimeTypeModel.Default.Add( typeof( Rectangle ) , false ).Add( "Position" , "Width" , "Height" );
		}
	}
}
