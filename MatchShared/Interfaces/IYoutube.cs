using System;

namespace MatchTracker
{
	public interface IYoutube
	{
		String youtubeUrl { get; set; }
		VideoType videoType { get; set; }
	}
}