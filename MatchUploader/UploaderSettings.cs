using System;
using System.Collections.Generic;

namespace MatchUploader
{
	public class UploaderSettings
	{
		public List<PendingUpload> pendingUploads { get; set; } = new List<PendingUpload>();

		public float uploadSpeed { get; set; } = 0; //in kylobytes per seconds, 0 means no throttling
		public GoogleSecrets secrets { get; set; }
		public KeyValueDataStore dataStore { get; set; }
		public Uri youtubeChannel { get; set; }
		public int retryCount { get; set; } = 5;
		public String gitEmail { get; set; }
		public String gitUsername { get; set; }
		public String gitPassword { get; set; }
	}
}
