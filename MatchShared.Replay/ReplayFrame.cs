using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker.Replay
{
	/// <summary>
	/// Represents a drawn frame from duck game and contains
	/// all the draw calls that were done in this frame
	/// </summary>
	public class ReplayFrame
	{
		//public List<ReplayDrawnItem> DrawCalls { get; set; } = new List<ReplayDrawnItem>();

		//TODO: turn the list into a hashset
		public Dictionary<int , List<ReplayDrawnItem>> DrawCalls { get; set; } = new Dictionary<int , List<ReplayDrawnItem>>();
		public TimeSpan Time { get; set; }
	}
}
