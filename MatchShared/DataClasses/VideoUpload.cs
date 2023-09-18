using System;

namespace MatchTracker
{
	public class VideoUpload
	{
		public string Url { get; set; }
		public VideoServiceType ServiceType { get; set; }
		public VideoUrlType VideoType { get; set; }
		public RecordingType RecordingType { get; set; }

		public bool IsPending() => VideoType != VideoUrlType.None && string.IsNullOrEmpty( Url );
	}
}
