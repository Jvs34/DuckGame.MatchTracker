using System;

namespace MatchUploader
{
	public class PendingUpload
	{
		public int ErrorCount { get; set; }
		public long FileSize { get; set; }
		public string LastException { get; set; }
		public Uri UploadUrl { get; set; }
		public string VideoName { get; set; }

		public PendingUpload()
		{
			VideoName = string.Empty;
			UploadUrl = null;
			LastException = string.Empty;
			ErrorCount = 0;
			FileSize = 0;
		}
	}
}