using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;

namespace MatchUploader
{
    public class UploaderSettings
    {
		public String lastUploadToResume = null; //this is the round name itself, we can get the path to the video file later
		public Uri uploadToResume = null;
		public float uploadSpeed = 0; //in kylobytes per seconds, 0 means no throttling
		public ClientSecrets secrets;
	}
}
