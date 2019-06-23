using System;

namespace MatchUploader
{
	public class PendingUpload
	{
		public int ErrorCount { get; set; }
		public long FileSize { get; set; }
		public long BytesSent { get; set; }
		public string LastException { get; set; }
		public Uri UploadUrl { get; set; }
		public string DataName { get; set; }
	}
}