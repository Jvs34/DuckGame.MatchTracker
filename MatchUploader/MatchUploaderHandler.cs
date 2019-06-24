using DSharpPlus;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

/*
Goes through all the folders, puts all rounds and matches into data.json
Also returns match/round data from the timestamped name and whatnot
*/

namespace MatchUploader
{
	public sealed class MatchUploaderHandler
	{
		private BotSettings BotSettings { get; }
		private Branch CurrentBranch { get; }
		private Repository DatabaseRepository { get; }
		private DiscordClient DiscordClient { get; }
		private IGameDatabase GameDatabase { get; }
		private string SettingsFolder { get; }
		private UploaderSettings UploaderSettings { get; }
		private CalendarService CalendarService { get; set; }

		//private AuthenticationResult microsoftGraphCredentials;
		private IConfigurationRoot Configuration { get; }
		private Dictionary<VideoMirrorType , Uploader> VideoUploaders { get; } = new Dictionary<VideoMirrorType , Uploader>();
		private List<Uploader> Uploaders { get; } = new List<Uploader>();

		/// <summary>
		/// Only to be used by the youtube downloader
		/// </summary>
		private HttpClient NormalHttpClient { get; } = new HttpClient();

		private JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented
		};

		public MatchUploaderHandler( string [] args )
		{
			GameDatabase = new FileSystemGameDatabase();

			UploaderSettings = new UploaderSettings();
			BotSettings = new BotSettings();

			SettingsFolder = Path.Combine( Directory.GetCurrentDirectory() , "Settings" );
			Configuration = new ConfigurationBuilder()
				.SetBasePath( SettingsFolder )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "uploader.json" )
				.AddJsonFile( "bot.json" )
				.AddCommandLine( args )
			.Build();


			Configuration.Bind( GameDatabase.SharedSettings );
			Configuration.Bind( UploaderSettings );
			Configuration.Bind( BotSettings );

			if( Repository.IsValid( GameDatabase.SharedSettings.GetRecordingFolder() ) )
			{
				Console.WriteLine( "Loaded {0}" , GameDatabase.SharedSettings.GetRecordingFolder() );
				DatabaseRepository = new Repository( GameDatabase.SharedSettings.GetRecordingFolder() );
				CurrentBranch = DatabaseRepository.Branches.First( branch => branch.IsCurrentRepositoryHead );
			}

			if( !string.IsNullOrEmpty( BotSettings.DiscordToken ) )
			{
				DiscordClient = new DiscordClient( new DiscordConfiguration()
				{
					AutoReconnect = true ,
					TokenType = TokenType.Bot ,
					Token = BotSettings.DiscordToken ,
				} );
			}

			CreateUploaders();
		}

		private void CreateUploaders()
		{
			//always create the calendar one
			{
				Uploader calendar = new CalendarUploader( new UploaderInfo() , GameDatabase , UploaderSettings );
				Uploaders.Add( calendar );
			}

			//youtube uploader
			switch( UploaderSettings.VideoMirrorUpload )
			{
				case VideoMirrorType.Youtube:
					{
						if( !UploaderSettings.UploadersInfo.TryGetValue( VideoMirrorType.Youtube , out UploaderInfo uploaderInfo ) )
						{
							uploaderInfo = new UploaderInfo()
							{
								UploaderType = VideoMirrorType.Youtube
							};

							UploaderSettings.UploadersInfo.TryAdd( VideoMirrorType.Youtube , uploaderInfo );
						}

						Uploader youtube = new YoutubeUploader( uploaderInfo , GameDatabase , UploaderSettings );

						Uploaders.Add( youtube );

						VideoUploaders.TryAdd( VideoMirrorType.Youtube , youtube );
					}
					break;

				case VideoMirrorType.Discord:
					{

					}
					break;

				default:
					break;
			}




		}

		public async Task CommitGitChanges()
		{
			await Task.CompletedTask;

			if( DatabaseRepository == null )
			{
				return;
			}

			Signature us = new Signature( Assembly.GetEntryAssembly().GetName().Name , UploaderSettings.GitEmail , DateTime.Now );
			var credentialsHandler = new CredentialsHandler(
				( _ , __ , ___ ) =>
					new UsernamePasswordCredentials()
					{
						Username = UploaderSettings.GitUsername ,
						Password = UploaderSettings.GitPassword ,
					}
			);


			Console.WriteLine( "Fetching repository status" );

			var mergeResult = Commands.Pull( DatabaseRepository , us , new PullOptions()
			{
				FetchOptions = new FetchOptions()
				{
					CredentialsProvider = credentialsHandler ,
				} ,
				MergeOptions = new MergeOptions()
				{
					CommitOnSuccess = true ,
				}
			} );

			if( mergeResult.Status == MergeStatus.Conflicts )
			{
				throw new Exception( "Could not complete a successful merge. " );
			}

			try
			{
				Commands.Stage( DatabaseRepository , "*" );
				DatabaseRepository.Commit( "Updated database" , us , us );

				Console.WriteLine( "Creating commit" );

				//I guess you should always try to push regardless if there has been any changes
				PushOptions pushOptions = new PushOptions
				{
					CredentialsProvider = credentialsHandler ,
				};
				DatabaseRepository.Network.Push( CurrentBranch , pushOptions );
				Console.WriteLine( "Commit pushed" );
			}
			catch( Exception e )
			{
				Console.WriteLine( e.Message );
			}

		}

		public async Task Initialize()
		{
			foreach( var uploader in Uploaders )
			{
				uploader.SaveSettingsCallback += SaveSettings;
				uploader.UpdateStatusCallback += SetDiscordPresence;

				if( !uploader.Info.HasBeenSetup )
				{
					uploader.CreateDefaultInfo();
					uploader.Info.HasBeenSetup = true;
				}

				await uploader.Initialize();
			}


			if( DiscordClient != null )
			{
				await DiscordClient.ConnectAsync();
				await DiscordClient.InitializeAsync();
			}

			string appName = Assembly.GetEntryAssembly().GetName().Name;

			//calendar stuff

			if( CalendarService == null )
			{
				CalendarService = new CalendarService( new BaseClientService.Initializer()
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

			/*
			Microsoft.Graph.GraphServiceClient graphService = new Microsoft.Graph.GraphServiceClient(
					"https://graph.microsoft.com/v1.0/" ,
					new Microsoft.Graph.DelegateAuthenticationProvider(
						async ( requestMessage ) => requestMessage.Headers.Authorization = new AuthenticationHeaderValue( "bearer" , userCredentials.AccessToken )
					)
				);
			*/
		}

		public async Task<List<Event>> GetAllCalendarEvents()
		{
			var allEvents = new List<Event>();

			var eventRequest = CalendarService.Events.List( UploaderSettings.CalendarID );
			Events eventResponse;

			do
			{
				eventResponse = await eventRequest.ExecuteAsync();

				foreach( var eventItem in eventResponse.Items )
				{
					if( !allEvents.Contains( eventItem ) )
					{
						allEvents.Add( eventItem );
					}
				}

				eventRequest.PageToken = eventResponse.NextPageToken;
			}
			while( eventResponse.Items.Count > 0 && eventResponse.NextPageToken != null );

			return allEvents;
		}

		public async Task HandleCalendar()
		{
			if( CalendarService is null )
			{
				return;
			}

			string calendarID = UploaderSettings.CalendarID;

			var allEvents = await GetAllCalendarEvents();

			List<Task<Event>> matchTasks = new List<Task<Event>>();

			foreach( string matchName in await GameDatabase.GetAll<MatchData>() )
			{
				string strippedName = GetStrippedMatchName( await GameDatabase.GetData<MatchData>( matchName ) );

				//if this event is already added, don't even call this

				if( allEvents.Any( x => x.Id.Equals( strippedName ) ) )
				{
					continue;
				}

				matchTasks.Add( GetCalendarEventForMatch( matchName ) );
			}

			await Task.WhenAll( matchTasks );

			//only get the first one and try using it

			List<Task<Event>> eventTasks = new List<Task<Event>>();

			foreach( var matchTask in matchTasks )
			{
				eventTasks.Add( CalendarService.Events.Insert( matchTask.Result , calendarID ).ExecuteAsync() );
			}

			//now create one for each one
			await Task.WhenAll( eventTasks );
		}

		private string GetStrippedMatchName( MatchData matchData )
		{
			return matchData.Name.Replace( "-" , string.Empty ).Replace( " " , string.Empty );//matchData.TimeStarted.ToString( "yyyyMMddHHmmss" );
		}

		public async Task<Event> GetCalendarEventForMatch( string matchName )
		{
			MatchData matchData = await GameDatabase.GetData<MatchData>( matchName );

			var playerWinners = await GameDatabase.GetAllData<PlayerData>( matchData.GetWinners().ToArray() );

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
				Description = string.Format( "Recorded on {0}\nThe winner is {1}" , GameDatabase.SharedSettings.DateTimeToString( matchData.TimeStarted ) , winner )
			};
		}

		public async Task LoadDatabase()
		{
			await GameDatabase.Load();
			Console.WriteLine( "Finished loading the database" );
		}

		public async Task RunAsync()
		{
			await LoadDatabase();
			await Initialize();

			try
			{
				await HandleCalendar();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			SaveSettings();

			await CommitGitChanges();

			await Upload();

			UploaderSettings.LastRan = DateTime.Now;

			SaveSettings();
		}

		private async Task Upload()
		{
			foreach( var uploader in Uploaders )
			{
				await uploader.UploadAll();
			}
		}

		//in this context, settings are only the uploaderSettings
		public void SaveSettings()
		{
			using( var writer = File.CreateText( Path.Combine( SettingsFolder , "uploader.json" ) ) )
			{
				Serializer.Serialize( writer , UploaderSettings );
			}
		}

		public async Task UploadToDiscordAsync()
		{
			var uploadChannel = await DiscordClient.GetChannelAsync( UploaderSettings.DiscordUploadChannel );

			if( uploadChannel == null )
			{
				Console.WriteLine( "Discord Upload channel is null" );
				return;
			}

			var ytClient = new YoutubeExplode.YoutubeClient( NormalHttpClient );

			//go through each round, see if they already have a discord mirror, otherwise reupload

			foreach( string roundName in await GameDatabase.GetAll<RoundData>() )
			{
				RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );
				if( !string.IsNullOrWhiteSpace( roundData.YoutubeUrl ) && roundData.VideoType == VideoType.VideoLink )
				{
					VideoMirrorData discordMirror = roundData.VideoMirrors.FirstOrDefault( mirror => mirror.MirrorType == VideoMirrorType.Discord );

					//see if it already has a discord mirror
					if( discordMirror != null ) //TODO: check if url is still valid?
					{
						continue;
					}

					Console.WriteLine( $"Uploading {roundName}" );

					//if it was successfull, save the data
					string discordMirrorUrl;

					var mediaStreamInfo = await ytClient.GetVideoMediaStreamInfosAsync( roundData.YoutubeUrl );
					//get the quality that actually fits into 8 mb
					var chosenQuality = mediaStreamInfo.Muxed.FirstOrDefault( quality => quality.Size <= UploaderSettings.DiscordMaxUploadSize );

					if( chosenQuality == null )
					{
						Console.WriteLine( $"Could not find a quality that fits into {UploaderSettings.DiscordMaxUploadSize} for {roundName}" );
						continue;
					}

					using( MemoryStream videoStream = new MemoryStream() )
					{
						await ytClient.DownloadMediaStreamAsync( chosenQuality , videoStream );
						videoStream.Position = 0;
						var message = await uploadChannel.SendFileAsync( $"{roundName}.mp4" , videoStream );
						discordMirrorUrl = message.Attachments.FirstOrDefault()?.Url;
					}

					if( !string.IsNullOrEmpty( discordMirrorUrl ) )
					{
						discordMirror = new VideoMirrorData()
						{
							MirrorType = VideoMirrorType.Discord ,
							URL = discordMirrorUrl ,
						};

						roundData.VideoMirrors.Add( discordMirror );
						await GameDatabase.SaveData( roundData );
						Console.WriteLine( $"Uploaded {roundName}" );
					}
				}
			}
		}


		private async Task SetDiscordPresence( string str )
		{
			if( DiscordClient == null || DiscordClient.CurrentUser == null )
			{
				return;
			}

			if( DiscordClient.CurrentUser.Presence.Activity != null && DiscordClient.CurrentUser.Presence.Activity.Name == str )
			{
				return;
			}

			await DiscordClient.UpdateStatusAsync( new DSharpPlus.Entities.DiscordActivity( str ) );
		}


		private async Task ProcessVideo( string roundName )
		{
			RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );

			string videoPath = GameDatabase.SharedSettings.GetRoundVideoPath( roundName );

			string outputPath = Path.ChangeExtension( videoPath , "converted.mp4" );

			if( roundData.RecordingType == RecordingType.Video && File.Exists( videoPath ) && !File.Exists( outputPath ) )
			{
				Console.WriteLine( $"Converting {roundName}" );

				IMediaInfo mediaInfo = await MediaInfo.Get( videoPath );
				IConversion newConversion = Conversion.New();

				bool abortConversion = mediaInfo.VideoStreams.Any( vid => vid.Bitrate < 6000000 );

				//only continue if these are videos with 6 megabits of bitrate
				if( abortConversion )
				{
					return;
				}

				foreach( var videostream in mediaInfo.VideoStreams )
				{
					newConversion.AddStream( videostream.SetCodec( VideoCodec.H264_nvenc ) );
				}

				foreach( var audiostream in mediaInfo.AudioStreams )
				{
					newConversion.AddStream( audiostream );
				}

				newConversion.SetOverwriteOutput( true );

				newConversion.SetOutput( outputPath );

				newConversion.AddParameter( "-crf 23 -maxrate 2000k -bufsize 4000k" );

				newConversion.SetPreset( ConversionPreset.Slow );
				Console.WriteLine( newConversion.Build() );
				await newConversion.Start();

				Console.WriteLine( outputPath );
			}
		}
		private async Task ProcessVideoFiles()
		{
			string tempFFmpegFolder = Path.Combine( Path.GetTempPath() , "ffmpeg" );

			if( !Directory.Exists( tempFFmpegFolder ) )
			{
				Directory.CreateDirectory( tempFFmpegFolder );
				Console.WriteLine( $"Created directory {tempFFmpegFolder}" );
			}

			FFmpeg.ExecutablesPath = tempFFmpegFolder;
			await FFmpeg.GetLatestVersion();


			List<Task> processingTasks = new List<Task>();

			foreach( string roundName in await GameDatabase.GetAll<RoundData>() )
			{
				processingTasks.Add( ProcessVideo( roundName ) );
			}

			await Task.WhenAll( processingTasks );
		}
	}
}