using System;

namespace MatchTracker
{
	public class VideoUpload : IVideoUpload
	{
		public string Url { get; set; }
		public string YoutubeUrl { get => Url; set => Url = value; }
		public VideoMirrorType MirrorType { get; set; }
		public VideoType VideoType { get; set; }
		public TimeSpan VideoStartTime { get; set; }
		public TimeSpan VideoEndTime { get; set; }

	}
}
