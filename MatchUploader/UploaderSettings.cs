using System;
using System.Collections.Generic;

namespace MatchUploader
{
	public class UploaderSettings
	{
		public KeyValueDataStore DataStore { get; set; }
		public string GitEmail { get; set; }
		public string GitPassword { get; set; }
		public string GitUsername { get; set; }
		public List<PendingUpload> PendingUploads { get; set; } = new List<PendingUpload>();

		public int RetryCount { get; set; } = 5;
		public GoogleSecrets Secrets { get; set; }
		public float UploadSpeed { get; set; } = 0; //in kylobytes per seconds, 0 means no throttling
		public Uri YoutubeChannel { get; set; }

		public string CalendarID { get; set; }
	}
}