using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using MatchTracker;
using System;
using System.Collections.Concurrent;
using System.Linq;
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
			await Task.CompletedTask;

			return new PendingUpload()
			{
				DataName = entry.DatabaseIndex ,
			};
		}

		protected override async Task FetchUploads()
		{
			ConcurrentBag<MatchData> uploadableRounds = new ConcurrentBag<MatchData>();
			await DB.IterateOverAll<MatchData>( async ( matchData ) =>
			{
				string matchEventID = GetStrippedMatchName( matchData );
				Event matchDataEvent = null;

				try
				{
					matchDataEvent = await Service.Events.Get( UploaderSettings.CalendarID , matchEventID ).ExecuteAsync();
				}
				catch( Exception )
				{
					Console.WriteLine( $"Event for {matchData.DatabaseIndex} does not exist" );
				}

				if( matchDataEvent is null )
				{
					uploadableRounds.Add( matchData );
				}

				return true;
			} );

			foreach( var matchData in uploadableRounds.OrderBy( matchData => matchData.TimeStarted ) )
			{
				PendingUpload upload = await CreatePendingUpload( matchData );

				if( upload != null )
				{
					Uploads.Enqueue( upload );
				}
			}

		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			Event matchEvent;
			try
			{
				matchEvent = await Service
					.Events
					.Insert( await GetCalendarEventForMatch( upload.DataName ) , UploaderSettings.CalendarID )
					.ExecuteAsync();
			}
			catch( Exception )
			{
				matchEvent = null;
			}

			return matchEvent != null;
		}

		private string GetStrippedMatchName( MatchData matchData )
		{
			return matchData.DatabaseIndex.Replace( "-" , string.Empty ).Replace( " " , string.Empty );//matchData.TimeStarted.ToString( "yyyyMMddHHmmss" );
		}

		public async Task<Event> GetCalendarEventForMatch( string matchName )
		{
			MatchData matchData = await DB.GetData<MatchData>( matchName );

			var playerWinners = await DB.GetAllData<PlayerData>( matchData.GetWinners().ToArray() );

			string winner = string.Join( " " , playerWinners.Select( x => x.GetName() ) );

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			return new Event()
			{
				Id = GetStrippedMatchName( matchData ) ,
				Start = new EventDateTime()
				{
					DateTime = matchData.TimeStarted
				} ,
				End = new EventDateTime()
				{
					DateTime = matchData.TimeEnded
				} ,
				Summary = $"{matchData.Name} {winner}" ,
				Description = string.Format( "Recorded on {0}\nThe winner is {1}" , DB.SharedSettings.DateTimeToString( matchData.TimeStarted ) , winner )
			};
		}

		public override void CreateDefaultInfo()
		{
			Info.HasApiLimit = false;
			Info.Retries = 0;
		}
	}
}
