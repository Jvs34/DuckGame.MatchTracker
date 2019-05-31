using System;
using System.Collections.Generic;
using System.Text;

namespace MatchTracker
{
	public class VideoMirrorData : IDatabaseEntry
	{
		public VideoMirrorType MirrorType { get; set; } = VideoMirrorType.Youtube;
		public string URL { get; set; }

		public string DatabaseIndex => URL;
	}
}
