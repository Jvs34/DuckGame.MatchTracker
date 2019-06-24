using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using MatchTracker;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MatchUploader
{
	public class CalendarUploader : Uploader
	{
		public CalendarService Service { get; private set; }

		public CalendarUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase , UploaderSettings settings ) : base( uploaderInfo , gameDatabase , settings )
		{
		}



		public override async Task Initialize()
		{
			string appName = GetType().Assembly.GetName().Name;

			Service = new CalendarService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( UploaderSettings.Secrets ,
					new [] { CalendarService.Scope.Calendar } ,
					"calendar" ,
					CancellationToken.None ,
					UploaderSettings.DataStore
				) ,
				ApplicationName = appName ,
				GZipEnabled = true ,
			} );
		}

		protected override async Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry )
		{
			PendingUpload upload = null;


			return upload;
		}

		protected override async Task FetchUploads()
		{
		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			Console.WriteLine( "Calendar uploader uploaded shit" );
			return true;
		}
	}
}
