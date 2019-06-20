using DSharpPlus;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;
using YoutubeExplode.Models.MediaStreams;

/*
Goes through all the folders, puts all rounds and matches into data.json
Also returns match/round data from the timestamped name and whatnot
*/

namespace MatchUploader
{
	public sealed class MatchUploaderHandler
	{
		private readonly BotSettings botSettings;
		private readonly Branch currentBranch;
		private readonly Repository databaseRepository;
		private readonly DiscordClient discordClient;
		private readonly IGameDatabase gameDatabase;
		private readonly string settingsFolder;
		private readonly UploaderSettings uploaderSettings;
		private PendingUpload currentVideo;
		private YouTubeService youtubeService;
		private CalendarService calendarService;

		//private AuthenticationResult microsoftGraphCredentials;
		private IConfigurationRoot Configuration { get; }


		/// <summary>
		/// Only to be used by the youtube downloader
		/// </summary>
		private HttpClient NormalHttpClient { get; } = new HttpClient();

		public MatchUploaderHandler( string [] args )
		{
			gameDatabase = new FileSystemGameDatabase();

			uploaderSettings = new UploaderSettings();
			botSettings = new BotSettings();

			settingsFolder = Path.Combine( Directory.GetCurrentDirectory() , "Settings" );
			Configuration = new ConfigurationBuilder()
				.SetBasePath( settingsFolder )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "uploader.json" )
				.AddJsonFile( "bot.json" )
				.AddCommandLine( args )
			.Build();

			Configuration.Bind( gameDatabase.SharedSettings );
			Configuration.Bind( uploaderSettings );
			Configuration.Bind( botSettings );

			if( Repository.IsValid( gameDatabase.SharedSettings.GetRecordingFolder() ) )
			{
				Console.WriteLine( "Loaded {0}" , gameDatabase.SharedSettings.GetRecordingFolder() );
				databaseRepository = new Repository( gameDatabase.SharedSettings.GetRecordingFolder() );
				currentBranch = databaseRepository.Branches.First( branch => branch.IsCurrentRepositoryHead );
			}

			if( !string.IsNullOrEmpty( botSettings.DiscordToken ) )
			{
				discordClient = new DiscordClient( new DiscordConfiguration()
				{
					AutoReconnect = true ,
					TokenType = TokenType.Bot ,
					Token = botSettings.DiscordToken ,
				} );
			}
		}

		public async Task AddRoundToPlaylist( string roundName , string matchName , List<PlaylistItem> playlistItems )
		{
			MatchData matchData = await gameDatabase.GetData<MatchData>( matchName );
			RoundData roundData = await gameDatabase.GetData<RoundData>( roundName );

			if( matchData.YoutubeUrl == null || roundData.YoutubeUrl == null )
			{
				Console.WriteLine( $"Could not add round {roundData.Name} to playlist because either match url or round url are missing" );
				await Task.CompletedTask;
				return;
			}

			int roundIndex = matchData.Rounds.IndexOf( roundData.Name );

			try
			{
				if( !playlistItems.Any( x => x.Snippet.ResourceId.VideoId == roundData.YoutubeUrl ) )
				{
					Console.WriteLine( $"Could not find {roundData.Name} on playlist {matchData.Name}, adding" );
					PlaylistItem roundPlaylistItem = await GetPlaylistItemForRound( roundData );
					roundPlaylistItem.Snippet.Position = roundIndex + 1;
					roundPlaylistItem.Snippet.PlaylistId = matchData.YoutubeUrl;
					await youtubeService.PlaylistItems.Insert( roundPlaylistItem , "snippet" ).ExecuteAsync();
				}
			}
			catch( Google.GoogleApiException e )
			{
				Console.WriteLine( e.Message );
			}
		}

		public async Task CleanPlaylists()
		{
			var allplaylists = await GetAllPlaylists();
			foreach( var playlist in allplaylists )
			{
				List<Task> playlistTasks = new List<Task>();

				var allvideos = await GetAllPlaylistItems( playlist.Id );
				foreach( var item in allvideos )
				{
					//playlistTasks.Add( youtubeService.PlaylistItems.Delete( item.Id ).ExecuteAsync() );

					try
					{
						await youtubeService.PlaylistItems.Delete( item.Id ).ExecuteAsync();
					}
					catch( Exception )
					{
						Console.WriteLine( $"Could not delete {item.Snippet.Title} from {playlist.Snippet.Title}" );
					}
				}

				if( playlistTasks.Count > 0 )
				{
					try
					{
						await Task.WhenAll( playlistTasks );
					}
					catch( Exception ex )
					{
						Console.WriteLine( ex );
					}
				}
			}
		}

		public async Task CommitGitChanges()
		{
			await Task.CompletedTask;

			if( databaseRepository == null )
				return;

			Signature us = new Signature( Assembly.GetEntryAssembly().GetName().Name , uploaderSettings.GitEmail , DateTime.Now );
			var credentialsHandler = new CredentialsHandler(
				( url , usernameFromUrl , supportedCredentialTypes ) =>
					new UsernamePasswordCredentials()
					{
						Username = uploaderSettings.GitUsername ,
						Password = uploaderSettings.GitPassword ,
					}
			);

			bool hasChanges = false;

			Console.WriteLine( "Fetching repository status" );

			var mergeResult = Commands.Pull( databaseRepository , us , new PullOptions()
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

			foreach( var item in databaseRepository.RetrieveStatus() )
			{
				if( item.State != FileStatus.Ignored && item.State != FileStatus.Unaltered )
				{
					Console.WriteLine( "File {0} {1}" , item.FilePath , item.State );

					Commands.Stage( databaseRepository , item.FilePath );
					hasChanges = true;
				}
			}

			if( hasChanges )
			{
				//Commands.Stage( repository , "*" );

				databaseRepository.Commit( "Updated database" , us , us );

				Console.WriteLine( "Creating commit" );

				//I guess you should always try to push regardless if there has been any changes
				PushOptions pushOptions = new PushOptions
				{
					CredentialsProvider = credentialsHandler ,
				};
				databaseRepository.Network.Push( currentBranch , pushOptions );
				Console.WriteLine( "Commit pushed" );
			}
		}

		public async Task<Playlist> CreatePlaylist( MatchData matchData )
		{
			try
			{
				Playlist pl = await GetPlaylistDataForMatch( matchData );
				var createPlaylistRequest = youtubeService.Playlists.Insert( pl , "snippet,status" );
				Playlist matchPlaylist = await createPlaylistRequest.ExecuteAsync();
				if( matchPlaylist != null )
				{
					matchData.YoutubeUrl = matchPlaylist.Id;
				}

				return matchPlaylist;
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			return null;
		}

		public async Task DoLogin()
		{
			if( discordClient != null )
			{
				await discordClient.ConnectAsync();
				await discordClient.InitializeAsync();
			}

			string appName = Assembly.GetEntryAssembly().GetName().Name;

			//youtube stuff

			if( youtubeService == null )
			{
				youtubeService = new YouTubeService( new BaseClientService.Initializer()
				{
					HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( uploaderSettings.Secrets ,
						new [] { YouTubeService.Scope.Youtube } ,
						"youtube" ,
						CancellationToken.None ,
						uploaderSettings.DataStore
					) ,
					ApplicationName = appName ,
					GZipEnabled = true ,
				} );
				youtubeService.HttpClient.Timeout = TimeSpan.FromMinutes( 2 );
			}
			//calendar stuff

			if( calendarService == null )
			{
				calendarService = new CalendarService( new BaseClientService.Initializer()
				{
					HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( uploaderSettings.Secrets ,
						new [] { CalendarService.Scope.Calendar } ,
						"calendar" ,
						CancellationToken.None ,
						uploaderSettings.DataStore
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

			var eventRequest = calendarService.Events.List( uploaderSettings.CalendarID );
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
			if( calendarService is null )
			{
				return;
			}

			string calendarID = uploaderSettings.CalendarID;

			var allEvents = await GetAllCalendarEvents();

			GlobalData globalData = await gameDatabase.GetData<GlobalData>();

			List<Task<Event>> matchTasks = new List<Task<Event>>();

			foreach( string matchName in await gameDatabase.GetAll<MatchData>() )
			{
				string strippedName = GetStrippedMatchName( await gameDatabase.GetData<MatchData>( matchName ) );

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
				eventTasks.Add( calendarService.Events.Insert( matchTask.Result , calendarID ).ExecuteAsync() );
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
			MatchData matchData = await gameDatabase.GetData<MatchData>( matchName );

			var youtubeSnippet = await GetPlaylistDataForMatch( matchData );

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
				Summary = youtubeSnippet.Snippet.Title ,
				Description = youtubeSnippet.Snippet.Description ,
			};
		}


		public async Task<List<PlaylistItem>> GetAllPlaylistItems( string playlistId )
		{
			List<PlaylistItem> allplaylistitems = new List<PlaylistItem>();
			var playlistItemsRequest = youtubeService.PlaylistItems.List( "snippet" );
			playlistItemsRequest.PlaylistId = playlistId;
			playlistItemsRequest.MaxResults = 50;

			PlaylistItemListResponse playlistItemListResponse = null;

			do
			{
				playlistItemListResponse = await playlistItemsRequest.ExecuteAsync();
				foreach( var plitem in playlistItemListResponse.Items )
				{
					if( !allplaylistitems.Any( x => x.Id == plitem.Id ) )
					{
						allplaylistitems.Add( plitem );
					}
				}
				playlistItemsRequest.PageToken = playlistItemListResponse.NextPageToken;
			}
			while( playlistItemListResponse.Items.Count > 0 && playlistItemListResponse.NextPageToken != null );

			return allplaylistitems;
		}

		public async Task<List<Playlist>> GetAllPlaylists()
		{
			List<Playlist> allplaylists = new List<Playlist>();
			var playlistsRequest = youtubeService.Playlists.List( "snippet" );
			playlistsRequest.Mine = true;
			playlistsRequest.MaxResults = 50;
			PlaylistListResponse playlistResponse;
			do
			{
				playlistResponse = await playlistsRequest.ExecuteAsync();

				//try to aggregate playlists until the response gives 0 videos
				foreach( var plitem in playlistResponse.Items )
				{
					if( !allplaylists.Contains( plitem ) )
					{
						allplaylists.Add( plitem );
					}
				}
				playlistsRequest.PageToken = playlistResponse.NextPageToken;
			}
			while( playlistResponse.Items.Count > 0 && playlistResponse.NextPageToken != null );
			return allplaylists;
		}

		public async Task<Playlist> GetPlaylistDataForMatch( MatchData matchData )
		{
			await Task.CompletedTask;

			string winner = matchData.GetWinnerName();

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			return new Playlist()
			{
				Snippet = new PlaylistSnippet()
				{
					Title = $"{matchData.Name} {winner}" ,
					Description = string.Format( "Recorded on {0}\nThe winner is {1}" , gameDatabase.SharedSettings.DateTimeToString( matchData.TimeStarted ) , winner ) ,
					Tags = new List<string>() { "duckgame" , "peniscorp" }
				} ,
				Status = new PlaylistStatus()
				{
					PrivacyStatus = "public"
				}
			};
		}

		public async Task<PlaylistItem> GetPlaylistItemForRound( RoundData roundData )
		{
			await Task.CompletedTask;
			return new PlaylistItem()
			{
				Snippet = new PlaylistItemSnippet()
				{
					ResourceId = new ResourceId()
					{
						Kind = "youtube#video" ,
						VideoId = roundData.YoutubeUrl ,
					}
				} ,
			};
		}

		public async Task<Video> GetVideoDataForRound( RoundData roundData )
		{
			await Task.CompletedTask;
			string winner = roundData.GetWinnerName();

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			string description = string.Format( "Recorded on {0}\nThe winner is {1}" , gameDatabase.SharedSettings.DateTimeToString( roundData.TimeStarted ) , winner );

			Video videoData = new Video()
			{
				Snippet = new VideoSnippet()
				{
					Title = $"{roundData.Name} {winner}" ,
					Tags = new List<string>() { "duckgame" , "peniscorp" } ,
					CategoryId = "20" , // See https://developers.google.com/youtube/v3/docs/videoCategories/list
					Description = description ,
				} ,
				Status = new VideoStatus()
				{
					PrivacyStatus = "unlisted" ,
				} ,
				RecordingDetails = new VideoRecordingDetails()
				{
					RecordingDate = roundData.TimeStarted ,
				}
			};

			return videoData;
		}

		public async Task LoadDatabase()
		{
			await gameDatabase.Load();
			Console.WriteLine( "Finished loading the database" );
		}

		public async Task RunAsync()
		{
			await LoadDatabase();
			await DoLogin();

			try
			{
				await HandleCalendar();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			await SaveSettings();

			await CommitGitChanges();
			await UploadAllRounds();

			uploaderSettings.LastRan = DateTime.Now;

			await SaveSettings();
		}

		//in this context, settings are only the uploaderSettings
		public async Task SaveSettings()
		{
			await File.WriteAllTextAsync(
				Path.Combine( settingsFolder , "uploader.json" ) ,
				JsonConvert.SerializeObject( uploaderSettings , Formatting.Indented )
			);
		}

		private async Task<IEnumerable<RoundData>> GetUploadableRounds()
		{
			ConcurrentBag<RoundData> uploadableRounds = new ConcurrentBag<RoundData>();

			await gameDatabase.IterateOverAllRoundsOrMatches( false , async ( round ) =>
			{
				if( uploadableRounds.Count >= 100 )
				{
					return false;
				}

				RoundData roundData = (RoundData) round;

				if( roundData.RecordingType == RecordingType.Video && string.IsNullOrEmpty( roundData.YoutubeUrl ) && File.Exists( gameDatabase.SharedSettings.GetRoundVideoPath( roundData.Name ) ) )
				{
					uploadableRounds.Add( roundData );
				}

				await Task.CompletedTask;
				return true;
			} );

			return uploadableRounds.OrderBy( roundData => roundData.TimeStarted );
		}

		public async Task UploadAllRounds()
		{
			Console.WriteLine( "Starting {0} uploads" , uploaderSettings.VideoMirrorUpload.ToString() );

			switch( uploaderSettings.VideoMirrorUpload )
			{
				//default youtube upload
				case VideoMirrorType.Youtube:
					{
						await UploadToYoutubeAsync();
						break;
					}
				case VideoMirrorType.Discord:
					{
						await UploadToDiscordAsync();
						break;
					}
				case VideoMirrorType.OneDrive:
					{
						await UploadToOneDriveAsync();
						break;
					}
				default:
					{
						Console.WriteLine( $"Unhandled mirror upload behaviour: {uploaderSettings.VideoMirrorUpload}" );
						break;
					}
			}

			await CommitGitChanges();
		}

		private async Task UploadToOneDriveAsync()
		{

		}

		public async Task UploadToDiscordAsync()
		{
			var uploadChannel = await discordClient.GetChannelAsync( uploaderSettings.DiscordUploadChannel );

			if( uploadChannel == null )
			{
				Console.WriteLine( "Discord Upload channel is null" );
				return;
			}

			var ytClient = new YoutubeExplode.YoutubeClient( NormalHttpClient );

			//just test with the first one for now
			GlobalData globalData = await gameDatabase.GetData<GlobalData>();

			//go through each round, see if they already have a discord mirror, otherwise reupload

			foreach( string roundName in await gameDatabase.GetAll<RoundData>() )
			{
				RoundData roundData = await gameDatabase.GetData<RoundData>( roundName );
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
					var chosenQuality = mediaStreamInfo.Muxed.FirstOrDefault( quality => quality.Size <= uploaderSettings.DiscordMaxUploadSize );

					if( chosenQuality == null )
					{
						Console.WriteLine( $"Could not find a quality that fits into {uploaderSettings.DiscordMaxUploadSize} for {roundName}" );
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
						await gameDatabase.SaveData( roundData );
						Console.WriteLine( $"Uploaded {roundName}" );
					}
				}
			}
		}

		public async Task UploadToYoutubeAsync()
		{
			var roundsToUpload = await GetUploadableRounds();

			int remaining = roundsToUpload.Count();

			foreach( RoundData roundData in roundsToUpload )
			{
				await UpdateUploadProgress( remaining );

				MatchData matchData = ( !string.IsNullOrEmpty( roundData.MatchName ) ) ? await gameDatabase.GetData<MatchData>( roundData.MatchName ) : null;
				List<PlaylistItem> playlistItems = null;

				if( matchData != null && string.IsNullOrEmpty( matchData.YoutubeUrl ) )
				{
					Playlist playlist = await CreatePlaylist( matchData );
					if( playlist != null )
					{
						await gameDatabase.SaveData( matchData );
					}
				}

				if( matchData != null && matchData.YoutubeUrl != null )
				{
					try
					{
						playlistItems = await GetAllPlaylistItems( matchData.YoutubeUrl );
					}
					catch( Exception )
					{
						playlistItems = null;
					}
				}

				bool isUploaded = await UploadRoundToYoutubeAsync( roundData.Name );

				if( isUploaded )
				{
					await RemoveVideoFile( roundData.Name );
					remaining--;

					if( matchData != null && playlistItems != null )
					{
						await AddRoundToPlaylist( roundData.Name , matchData.Name , playlistItems );
					}
				}
			}
		}


		public async Task<bool> UploadRoundToYoutubeAsync( string roundName )
		{
			RoundData roundData = await gameDatabase.GetData<RoundData>( roundName );

			if( youtubeService == null )
			{
				throw new NullReferenceException( "Youtube service is not initialized!!!" );
			}

			if( roundData.YoutubeUrl != null )
			{
				return false;
			}

			Video videoData = await GetVideoDataForRound( roundData );
			string filePath = gameDatabase.SharedSettings.GetRoundVideoPath( roundData.Name );

			if( !File.Exists( filePath ) )
			{
				throw new ArgumentNullException( $"{roundData.Name} does not contain a video!" );
			}

			string reEncodedVideoPath = Path.ChangeExtension( filePath , "converted.mp4" );

			if( File.Exists( reEncodedVideoPath ) )
			{
				filePath = reEncodedVideoPath;
			}

			using( var fileStream = new FileStream( filePath , FileMode.Open ) )
			{
				//get the pending upload for this roundName
				currentVideo = uploaderSettings.PendingUploads.Find( x => x.VideoName.Equals( roundData.Name ) );

				if( currentVideo == null )
				{
					currentVideo = new PendingUpload()
					{
						VideoName = roundData.Name
					};

					uploaderSettings.PendingUploads.Add( currentVideo );
				}

				currentVideo.FileSize = fileStream.Length;

				if( currentVideo.ErrorCount > uploaderSettings.RetryCount )
				{
					currentVideo.UploadUrl = null;
					Console.WriteLine( "Replacing resumable upload url for {0} after too many errors" , currentVideo.VideoName );
					currentVideo.ErrorCount = 0;
					currentVideo.LastException = string.Empty;
				}

				//TODO:Maybe it's possible to create a throttable request by extending the class of this one and initializing it with this one's values
				var videosInsertRequest = youtubeService.Videos.Insert( videoData , "snippet,status,recordingDetails" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videosInsertRequest.ProgressChanged += OnUploadProgress;
				videosInsertRequest.ResponseReceived += OnResponseReceived;
				videosInsertRequest.UploadSessionData += OnStartUploading;

				IUploadProgress uploadProgress;

				if( currentVideo.UploadUrl != null )
				{
					Console.WriteLine( "Resuming upload {0}" , currentVideo.VideoName );
					uploadProgress = await videosInsertRequest.ResumeAsync( currentVideo.UploadUrl );
				}
				else
				{
					Console.WriteLine( "Beginning to upload {0}" , currentVideo.VideoName );
					uploadProgress = await videosInsertRequest.UploadAsync();
				}

				//save it to the uploader settings and increment the error count only if it's not the annoying too many videos error
				if( uploadProgress.Status != UploadStatus.Completed && currentVideo.UploadUrl != null )
				{
					currentVideo.LastException = uploadProgress.Exception.Message;
					currentVideo.ErrorCount++;
					await SaveSettings();
				}
				currentVideo = null;
				return uploadProgress.Status == UploadStatus.Completed;
			}
		}

		private async Task SearchMap( string mapGuid )
		{
			GlobalData globalData = await gameDatabase.GetData<GlobalData>();
			await gameDatabase.IterateOverAllRoundsOrMatches( false , async ( round ) =>
			{
				RoundData roundData = (RoundData) round;

				if( !string.IsNullOrEmpty( roundData.YoutubeUrl ) && roundData.LevelName == mapGuid )
				{
					string youtubeUrl = $"https://www.youtube.com/watch?v={roundData.YoutubeUrl}";
					Process.Start( "cmd" , $"/c start {youtubeUrl}" );
					return false;
				}


				await Task.CompletedTask;
				return true;
			} );
		}

		private async Task AddYoutubeIdToRound( string roundName , string videoId )
		{
			RoundData roundData = await gameDatabase.GetData<RoundData>( roundName );
			roundData.YoutubeUrl = videoId;
			await gameDatabase.SaveData( roundData );
		}


		private void OnResponseReceived( Video video )
		{
			if( uploaderSettings.PendingUploads.Contains( currentVideo ) )
			{
				uploaderSettings.PendingUploads.Remove( currentVideo );
			}

			SaveSettings().Wait();
			AddYoutubeIdToRound( currentVideo.VideoName , video.Id ).Wait();

			Console.WriteLine( "Round {0} with id {1} was successfully uploaded." , currentVideo.VideoName , video.Id );
		}

		private void OnStartUploading( IUploadSessionData resumable )
		{
			currentVideo.UploadUrl = resumable.UploadUri;
			SaveSettings().Wait();//save right away in case the program crashes or connection screws up
		}

		private void OnUploadProgress( IUploadProgress progress )
		{
			switch( progress.Status )
			{
				case UploadStatus.Uploading:
					{
						double percentage = Math.Round( ( (double) progress.BytesSent / (double) currentVideo.FileSize ) * 100f , 2 );
						//UpdateUploadProgress( percentage , true );
						Console.WriteLine( $"{currentVideo.VideoName} : {percentage}%" );
						break;
					}
				case UploadStatus.Failed:
					Console.WriteLine( "An error prevented the upload from completing. {0}" , progress.Exception );
					break;
			}
		}

		private async Task RemoveVideoFile( string roundName )
		{
			RoundData roundData = await gameDatabase.GetData<RoundData>( roundName );

			//don't accidentally delete stuff that somehow doesn't have a url set
			if( roundData.YoutubeUrl == null )
				return;

			try
			{
				string filePath = gameDatabase.SharedSettings.GetRoundVideoPath( roundName );
				string reEncodedFilePath = Path.ChangeExtension( filePath , "converted.mp4" );

				if( File.Exists( filePath ) )
				{
					Console.WriteLine( "Removed video file for {0}" , roundName );
					File.Delete( filePath );
				}

				if( File.Exists( reEncodedFilePath ) )
				{
					Console.WriteLine( "Also removing the reencoded version {0}" , roundName );
					File.Delete( reEncodedFilePath );
				}
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}
		}

		private async Task SetDiscordPresence( string str )
		{
			if( discordClient == null || discordClient.CurrentUser == null )
			{
				return;
			}


			if( discordClient.CurrentUser.Presence.Activity != null && discordClient.CurrentUser.Presence.Activity.Name == str )
			{
				return;
			}

			await discordClient.UpdateStatusAsync( new DSharpPlus.Entities.DiscordActivity( str ) );
		}

		private async Task UpdateUploadProgress( int remaining )
		{
			await SetDiscordPresence( $"{remaining} {uploaderSettings.VideoMirrorUpload.ToString()} videos to upload" );
		}

		private async Task ProcessVideo( string roundName )
		{
			RoundData roundData = await gameDatabase.GetData<RoundData>( roundName );

			string videoPath = gameDatabase.SharedSettings.GetRoundVideoPath( roundName );

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

			GlobalData globalData = await gameDatabase.GetData<GlobalData>();


			List<Task> processingTasks = new List<Task>();

			foreach( string roundName in await gameDatabase.GetAll<RoundData>() )
			{
				processingTasks.Add( ProcessVideo( roundName ) );
			}

			await Task.WhenAll( processingTasks );
		}
	}
}