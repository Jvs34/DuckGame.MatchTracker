using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	/// <summary>
	/// Represents a drawn frame from duck game and contains
	/// all the draw calls that were done in this frame
	/// </summary>
	public class ReplayFrame
	{
		public List<ReplayDrawnItem> DrawCalls { get; set; } = new List<ReplayDrawnItem>();
		public TimeSpan Time { get; set; }
	}
}
