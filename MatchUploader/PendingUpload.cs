using System;

namespace MatchUploader
{
	public class PendingUpload
	{
		public int errorCount { get; set; }
		public long fileSize { get; set; }
		public string lastException { get; set; }
		public Uri uploadUrl { get; set; }
		public string videoName { get; set; }

		public PendingUpload()
		{
			videoName = string.Empty;
			uploadUrl = null;
			lastException = string.Empty;
			errorCount = 0;
			fileSize = 0;
		}
	}
}