using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MatchTracker;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MatchUploader
{
	public class YoutubeUploader : Uploader
	{


		public YouTubeService Service { get; private set; }

		public YoutubeUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase , UploaderSettings settings ) : base( uploaderInfo , gameDatabase , settings )
		{
		}

		public override async Task Initialize()
		{
			string appName = GetType().Assembly.GetName().Name;
			Service = new YouTubeService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( UploaderSettings.Secrets ,
					new [] { YouTubeService.Scope.Youtube } ,
					"youtube" ,
					CancellationToken.None ,
					UploaderSettings.DataStore
				) ,
				ApplicationName = appName ,
				GZipEnabled = true ,
			} );
			Service.HttpClient.Timeout = TimeSpan.FromMinutes( 2 );
		}

		public override void CreateDefaultInfo()
		{
			Info.HasApiLimit = true;
			Info.NextReset = TimeSpan.FromHours( 24 );
			Info.Retries = 3;
			Info.UploadsBeforeReset = 100;//this might turn into 50 sometimes??verify
		}

		protected override async Task FetchUploads()
		{
			ConcurrentBag<RoundData> uploadableRounds = new ConcurrentBag<RoundData>();
			await DB.IterateOverAll<RoundData>( async ( roundData ) =>
			{
				await Task.CompletedTask;
				if( uploadableRounds.Count >= Info.UploadsBeforeReset )
				{
					return false;
				}

				if( roundData.RecordingType == RecordingType.Video && string.IsNullOrEmpty( roundData.YoutubeUrl ) )
				{
					uploadableRounds.Add( roundData );
				}
				return true;
			} );

			foreach( var roundData in uploadableRounds.OrderBy( roundData => roundData.TimeStarted ) )
			{
				PendingUpload upload = CreatePendingUpload( roundData );

				if( upload != null )
				{
					Uploads.Enqueue( upload );
				}
			}

		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{

			return true;
		}
	}
}
