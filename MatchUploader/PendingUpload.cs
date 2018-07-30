using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

namespace MatchUploader
{

	public class PendingUpload
	{
		public String videoName { get; set; }
		public Uri uploadUrl { get; set; }
		public String lastException { get; set; }
		public int errorCount { get; set; }
		public long fileSize { get; set; }

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
