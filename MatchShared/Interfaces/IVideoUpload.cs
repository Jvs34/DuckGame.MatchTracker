using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public interface IVideoUpload
	{
		string YoutubeUrl { get; set; }
		VideoUrlType VideoType { get; set; }

		/// <summary>
		/// In the case of a merged video, this will define at which point of the video
		/// the current round or match will start
		/// </summary>
		TimeSpan VideoStartTime { get; set; }

		/// <summary>
		/// Same as <see cref="VideoStartTime"/> but for when this tidbit ends
		/// </summary>
		TimeSpan VideoEndTime { get; set; }
	}
}