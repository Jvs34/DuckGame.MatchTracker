using System;

namespace MatchUploader
{
	public class PendingUpload
	{
		public int errorCount { get; set; }
		public long fileSize { get; set; }
		public String lastException { get; set; }
		public Uri uploadUrl { get; set; }
		public String videoName { get; set; }

		public PendingUpload()
		{
			videoName = String.Empty;
			uploadUrl = null;
			lastException = String.Empty;
			errorCount = 0;
			fileSize = 0;
		}
	}
}