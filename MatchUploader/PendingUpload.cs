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
		public String videoName;
		public Uri uploadUrl;
		public String lastException;
		public int errorCount;
		[JsonIgnore] public long fileSize;


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
