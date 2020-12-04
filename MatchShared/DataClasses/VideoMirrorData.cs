using System;

namespace MatchTracker
{
	public class VideoMirrorData
	{
		public VideoMirrorType MirrorType { get; set; }
		public string URL { get; set; }
		public VideoType VideoType { get; set; }
		
		/// <summary>
		/// In the case of a merged video, this will define at which point of the video
		/// the current round or match will start
		/// </summary>
		public TimeSpan VideoStartTime { get; set; }
		
		/// <summary>
		/// Same as <see cref="VideoStartTime"/> but for when this tidbit ends
		/// </summary>
		public TimeSpan VideoEndTime { get; set; }
	}
}
