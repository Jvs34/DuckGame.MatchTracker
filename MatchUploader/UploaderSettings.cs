using System;
using System.Collections.Generic;

namespace MatchUploader
{
	public class UploaderSettings
	{
		public KeyValueDataStore dataStore { get; set; }
		public String gitEmail { get; set; }
		public String gitPassword { get; set; }
		public String gitUsername { get; set; }
		public List<PendingUpload> pendingUploads { get; set; } = new List<PendingUpload>();

		public int retryCount { get; set; } = 5;
		public GoogleSecrets secrets { get; set; }
		public float uploadSpeed { get; set; } = 0; //in kylobytes per seconds, 0 means no throttling
		public Uri youtubeChannel { get; set; }

		public string calendarID { get; set; }
	}
}