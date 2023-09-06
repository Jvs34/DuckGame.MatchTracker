using Google.Apis.Auth.OAuth2;

namespace MatchUploader
{
	public class GoogleSecrets
	{
		public string client_id { get; set; }
		public string client_secret { get; set; }

		public static implicit operator ClientSecrets( GoogleSecrets secrets )
		{
			return new ClientSecrets()
			{
				ClientSecret = secrets.client_secret ,
				ClientId = secrets.client_id
			};
		}
	}
}