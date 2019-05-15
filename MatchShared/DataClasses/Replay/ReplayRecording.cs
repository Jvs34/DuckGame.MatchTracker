﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	/// <summary>
	/// Represents an actual recording of the match, along with all the frames
	/// and references to whatever textures and other stuff were used
	/// </summary>
	public class ReplayRecording
	{
		public int CurrentFrame { get; set; }
		public List<ReplayFrame> Frames { get; set; } = new List<ReplayFrame>();//TODO: change to a different list or array type?

		/// <summary>
		/// The list of textures that were used in this recording,
		/// so a replay viewer can cache them right away
		/// Also these will be referenced directly from a ReplayDrawnItem
		/// </summary>
		public List<string> Textures { get; set; } = new List<string>();
		public List<string> Materials { get; set; } = new List<string>();



		/// <summary>
		/// This is a quite expensive function, trims all the draw calls that were unchanged throughout
		/// all the replay frames, to shave repeated data
		/// </summary>
		public void TrimDrawCalls()
		{

		}

		public ReplayFrame StartFrame()
		{
			var replayFrame = new ReplayFrame();

			Frames.Add( replayFrame );

			return replayFrame;
		}

		public void AddDrawCall( string texture , Vec2 position , Rectangle? sourceRectangle , Color color , float rotation , Vec2 spriteCenter , Vec2 scale , int effects , double depth )
		{
			//Draw(Tex2D texture, Vec2 position, Rectangle? sourceRectangle, Color color, float rotation, Vec2 origin, Vec2 scale, SpriteEffects effects, Depth depth = default(Depth))
			ReplayFrame replayFrame = Frames [CurrentFrame];

			replayFrame.DrawCalls.Add( new ReplayDrawnItem()
			{
				Angle = rotation ,
				TexCoords = sourceRectangle ,
				Depth = depth ,
				Center = spriteCenter ,
				Position = position ,
				Scale = scale ,
				Color = color ,
				Texture = texture ,
				//TODO: materials along with their params
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