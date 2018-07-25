using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

namespace MatchUploader
{
	public class UploaderSettings
	{
		public List<PendingUpload> pendingUploads = new List<PendingUpload>();

		public float uploadSpeed = 0; //in kylobytes per seconds, 0 means no throttling
		public ClientSecrets secrets;
		public KeyValueDataStore dataStore;
		public Uri youtubeChannel;
		public Uri discordWebhook;
		public String discordClientId;
		public String discordToken;

		public int retryCount = 5;

		public String gitEmail;
		public String gitUsername;
		public String gitPassword;
	}
}
