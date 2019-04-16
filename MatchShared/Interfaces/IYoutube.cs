using System;

namespace MatchTracker
{
	public interface IYoutube
	{
		string youtubeUrl { get; set; }
		VideoType videoType { get; set; }
	}
}