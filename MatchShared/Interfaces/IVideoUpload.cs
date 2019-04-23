using System;
using System.Collections.Generic;

namespace MatchTracker
{
	public interface IVideoUpload
	{
		string YoutubeUrl { get; set; }
		VideoType VideoType { get; set; }
		List<VideoMirrorData> VideoMirrors { get; set; }
	}
}