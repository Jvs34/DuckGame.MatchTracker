using Google.Apis.Auth.OAuth2;
using System;

namespace MatchUploader
{
	public class GoogleSecrets
	{
		public String client_id { get; set; }
		public String client_secret { get; set; }

		public static implicit operator ClientSecrets( GoogleSecrets secrets )
		{
			return new ClientSecrets()
			{
				ClientSecret = secrets.client_secret,
				ClientId = secrets.client_id
			};
		}
	}
}
