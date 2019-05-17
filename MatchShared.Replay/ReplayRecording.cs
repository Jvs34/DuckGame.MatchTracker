using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		private int CurrentFrame { get; set; }
		public List<ReplayFrame> Frames { get; set; } = new List<ReplayFrame>();//TODO: change to a different list or array type?

		/// <summary>
		/// The list of textures that were used in this recording,
		/// so a replay viewer can cache them right away
		/// Also these will be referenced directly from a ReplayDrawnItem
		/// </summary>
		public List<string> Textures { get; set; } = new List<string>();
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

			if( !replayFrame.DrawCalls.TryGetValue( entityIndex , out List<ReplayDrawnItem> drawCalls ) )
			{
				drawCalls = new List<ReplayDrawnItem>();
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
	}
}
