using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;

namespace MatchUploader
{
    public class UploaderSettings
    {
		String lastUploadToResume = null; //this is the round name itself, we can get the path to the video file later
		Uri uploadToResume = null;
		float uploadSpeed = 0; //in kylobytes per seconds, 0 means no throttling
		ClientSecrets secrets = new ClientSecrets();
	}
}
