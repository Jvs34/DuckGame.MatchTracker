using System;

namespace MatchTracker
{
	public interface IYoutube
	{
		string YoutubeUrl { get; set; }
		VideoType VideoType { get; set; }
	}
}