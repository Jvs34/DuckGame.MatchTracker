using DSharpPlus;
using Google.Apis.Auth.OAuth2;
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
/*
Goes through all the folders, puts all rounds and matches into data.json
Also returns match/round data from the timestamped name and whatnot
*/

namespace MatchUploader
{
	public sealed class MatchUploaderHandler : IModeHandler
	{
		private IConfigurationRoot Configuration { get; }
		private String settingsFolder;
		private GameDatabase gameDatabase;
		private UploaderSettings uploaderSettings;
		private BotSettings botSettings;
		private YouTubeService youtubeService;
		private PendingUpload currentVideo;
		private Repository databaseRepository;
		private Branch currentBranch;
		private DiscordClient discordClient;


		public MatchUploaderHandler()
		{
			gameDatabase = new GameDatabase();
			gameDatabase.LoadGlobalDataDelegate += LoadDatabaseGlobalDataFile;
			gameDatabase.LoadMatchDataDelegate += LoadDatabaseMatchDataFile;
			gameDatabase.LoadRoundDataDelegate += LoadDatabaseRoundDataFile;
			gameDatabase.SaveGlobalDataDelegate += SaveDatabaseGlobalDataFile;
			gameDatabase.SaveMatchDataDelegate += SaveDatabaseMatchDataFile;
			gameDatabase.SaveRoundDataDelegate += SaveDatabaseRoundataFile;

			uploaderSettings = new UploaderSettings();
			botSettings = new BotSettings();

			settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			Configuration = new ConfigurationBuilder()
				.SetBasePath( settingsFolder )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "uploader.json" )
				.AddJsonFile( "bot.json" )
			.Build();

			Configuration.Bind( gameDatabase.sharedSettings );
			Configuration.Bind( uploaderSettings );
			Configuration.Bind( botSettings );

			if( Repository.IsValid( gameDatabase.sharedSettings.GetRecordingFolder() ) )
			{
				Console.WriteLine( "Loaded {0}" , gameDatabase.sharedSettings.GetRecordingFolder() );
				databaseRepository = new Repository( gameDatabase.sharedSettings.GetRecordingFolder() );
				currentBranch = databaseRepository.Branches.First( branch => branch.IsCurrentRepositoryHead );
			}

			if( !String.IsNullOrEmpty( botSettings.discordToken ) )
			{
				discordClient = new DiscordClient( new DiscordConfiguration()
				{
					AutoReconnect = true,
					TokenType = TokenType.Bot,
					Token = botSettings.discordToken,
				} );
			}
		}

		private async Task<GlobalData> LoadDatabaseGlobalDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			return JsonConvert.DeserializeObject<GlobalData>( await File.ReadAllTextAsync( sharedSettings.GetGlobalPath() ) );
		}

		private async Task<MatchData> LoadDatabaseMatchDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			return JsonConvert.DeserializeObject<MatchData>( await File.ReadAllTextAsync( sharedSettings.GetMatchPath( matchName ) ) );
		}

		private async Task<RoundData> LoadDatabaseRoundDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			return JsonConvert.DeserializeObject<RoundData>( await File.ReadAllTextAsync( sharedSettings.GetRoundPath( roundName ) ) );
		}

		private async Task SaveDatabaseGlobalDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , GlobalData globalData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}

		private async Task SaveDatabaseMatchDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , String matchName , MatchData matchData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented ) );
		}

		private async Task SaveDatabaseRoundataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , String roundName , RoundData roundData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented ) );
		}

		//in this context, settings are only the uploaderSettings
		public void SaveSettings()
		{
			File.WriteAllText(
				Path.Combine( settingsFolder , "uploader.json" ) ,
				JsonConvert.SerializeObject( uploaderSettings , Formatting.Indented )
			);
		}

		public async Task DoLogin()
		{
			if( discordClient != null )
			{
				await discordClient.ConnectAsync();
				await discordClient.InitializeAsync();
			}

			UserCredential uc = null;

			var permissions = new [] { YouTubeService.Scope.Youtube };

			//TODO: allow switching between users? is this needed?


			ClientSecrets secrets = new ClientSecrets()
			{
				ClientId = uploaderSettings.secrets.client_id ,
				ClientSecret = uploaderSettings.secrets.client_secret ,
			};

			uc = await GoogleWebAuthorizationBroker.AuthorizeAsync( secrets ,
				permissions ,
				"user" ,
				CancellationToken.None ,
				uploaderSettings.dataStore
			);
			

			youtubeService = new YouTubeService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = uc ,
				ApplicationName = Assembly.GetEntryAssembly().GetName().Name ,
				GZipEnabled = true ,
			} );

			youtubeService.HttpClient.Timeout = TimeSpan.FromMinutes( 2 );
		}

		//updates the global data.json
		public async Task UpdateGlobalData()
		{
			String roundsPath = Path.Combine( gameDatabase.sharedSettings.GetRecordingFolder() , gameDatabase.sharedSettings.roundsFolder );
			String matchesPath = Path.Combine( gameDatabase.sharedSettings.GetRecordingFolder() , gameDatabase.sharedSettings.matchesFolder );

			if( !Directory.Exists( gameDatabase.sharedSettings.GetRecordingFolder() ) || !Directory.Exists( roundsPath ) || !Directory.Exists( matchesPath ) )
			{
				throw new DirectoryNotFoundException( "Folders do not exist" );
			}

			String globalDataPath = gameDatabase.sharedSettings.GetGlobalPath();

			GlobalData globalData = new GlobalData();

			if( File.Exists( globalDataPath ) )
			{
				globalData = await gameDatabase.GetGlobalData();
			}

			var roundFolders = Directory.EnumerateDirectories( roundsPath );

			foreach( var folderPath in roundFolders )
			{
				//if it doesn't contain the folder, check if the round is valid
				String folderName = Path.GetFileName( folderPath );

				if( !globalData.rounds.Contains( folderName ) )
				{
					globalData.rounds.Add( folderName );
				}

				RoundData roundData = await gameDatabase.GetRoundData( folderName );
				if( String.IsNullOrEmpty( roundData.name ) )
				{
					roundData.name = gameDatabase.sharedSettings.DateTimeToString( roundData.timeStarted );
					Console.WriteLine( $"Adding roundName to roundData {roundData.name}" );

					await gameDatabase.SaveRoundData( roundData.name , roundData );
				}

				if( roundData.recordingType == RecordingType.None )
				{
					if( roundData.youtubeUrl != null || File.Exists( gameDatabase.sharedSettings.GetRoundVideoPath( roundData.name ) ) )
					{
						roundData.recordingType = RecordingType.Video;
						await gameDatabase.SaveRoundData( roundData.name , roundData );
					}
				}

				foreach( var roundPlayer in roundData.players )
				{
					//if this player is in the globaldata, we need to fill in the missing info

					foreach( var globalPly in globalData.players )
					{
						if( roundPlayer.Equals( globalPly ) )
						{
							roundPlayer.discordId = globalPly.discordId;
							roundPlayer.nickName = globalPly.nickName;
						}
					}

				}

				await gameDatabase.SaveRoundData( roundData.name , roundData );
			}

			var matchFiles = Directory.EnumerateFiles( matchesPath );

			foreach( var matchPath in matchFiles )
			{
				String matchName = Path.GetFileNameWithoutExtension( matchPath );

				if( !globalData.matches.Contains( matchName ) )
				{
					globalData.matches.Add( matchName );
				}

				MatchData matchData = await gameDatabase.GetMatchData( matchName );

				//while we're here, let's check if all the players are added to the global data too
				foreach( PlayerData ply in matchData.players )
				{
					if( !globalData.players.Any( p => p.userId == ply.userId ) )
					{
						PlayerData toAdd = ply;
						ply.team = null;

						globalData.players.Add( toAdd );
					}
				}

				foreach( var matchPlayer in matchData.players )
				{
					//if this player is in the globaldata, we need to fill in the missing info

					foreach( var globalPly in globalData.players )
					{
						if( matchPlayer.Equals( globalPly ) )
						{
							matchPlayer.discordId = globalPly.discordId;
							matchPlayer.nickName = globalPly.nickName;
						}
					}

				}

				if( String.IsNullOrEmpty( matchData.name ) )
				{
					matchData.name = gameDatabase.sharedSettings.DateTimeToString( matchData.timeStarted );
					Console.WriteLine( $"Adding matchName to matchData {matchData.name}" );
				}

				await gameDatabase.SaveMatchData( matchData.name , matchData );
			}

			await gameDatabase.SaveGlobalData( globalData );
		}

		public async Task LoadDatabase()
		{
			await gameDatabase.Load();
			Console.WriteLine( "Finished loading the database" );
		}

		public async Task<Video> GetVideoDataForRound( String roundName )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName );
			String winner = roundData.GetWinnerName();

			if( String.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			String description = String.Format( "Recorded on {0}\nThe winner is {1}" , gameDatabase.sharedSettings.DateTimeToString( roundData.timeStarted ) , winner );

			Video videoData = new Video()
			{
				Snippet = new VideoSnippet()
				{
					Title = $"{roundName} {winner}" ,
					Tags = new List<String>() { "duckgame" , "peniscorp" } ,
					CategoryId = "20" , // See https://developers.google.com/youtube/v3/docs/videoCategories/list
					Description = description ,
				} ,
				Status = new VideoStatus()
				{
					PrivacyStatus = "unlisted" ,
				} ,
				RecordingDetails = new VideoRecordingDetails()
				{
					RecordingDate = roundData.timeStarted ,
				}
			};

			return videoData;
		}

		public async Task<Playlist> GetPlaylistDataForMatch( String matchName )
		{
			MatchData matchData = await gameDatabase.GetMatchData( matchName );

			String winner = matchData.GetWinnerName();

			if( String.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			return new Playlist()
			{
				Snippet = new PlaylistSnippet()
				{
					Title = $"{matchName} {winner}" ,
					Description = String.Format( "Recorded on {0}\nThe winner is {1}" , gameDatabase.sharedSettings.DateTimeToString( matchData.timeStarted ) , winner ) ,
					Tags = new List<String>() { "duckgame" , "peniscorp" }
				} ,
				Status = new PlaylistStatus()
				{
					PrivacyStatus = "public"
				}
			};
		}

		public async Task<PlaylistItem> GetPlaylistItemForRound( String roundName )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName );
			return new PlaylistItem()
			{
				Snippet = new PlaylistItemSnippet()
				{
					ResourceId = new ResourceId()
					{
						Kind = "youtube#video" ,
						VideoId = roundData.youtubeUrl
					}
				} ,
			};
		}

		private async Task AddYoutubeIdToRound( String roundName , String videoId )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName );
			roundData.youtubeUrl = videoId;
			await gameDatabase.SaveRoundData( roundName , roundData );
		}

		public async Task UploadAllRounds()
		{
			Console.WriteLine( "Starting youtube uploads" );

			GlobalData globalData = await gameDatabase.GetGlobalData();

			int remaining = await GetRemainingFilesCount();

			foreach( String matchName in globalData.matches )
			{
				MatchData matchData = await gameDatabase.GetMatchData( matchName );
				List<PlaylistItem> playlistItems = await GetAllPlaylistItems( matchData.youtubeUrl );

				foreach( String roundName in matchData.rounds )
				{
					await UpdateUploadProgress( remaining );
					RoundData oldRoundData = await gameDatabase.GetRoundData( roundName );

					if( oldRoundData.recordingType != RecordingType.Video )
					{
						Console.WriteLine( $"Skipping {oldRoundData.name} as it's not a video" );
						continue;
					}

					bool isUploaded = oldRoundData.youtubeUrl != null;

					if( !isUploaded )
					{
						await UploadRoundToYoutubeAsync( roundName );
						RoundData roundData = await gameDatabase.GetRoundData( roundName );
						await RemoveVideoFile( roundName );
						if( !isUploaded && roundData.youtubeUrl != null )
						{
							remaining--;
							await AddRoundToPlaylist( roundName , matchName , playlistItems );
							CommitGitChanges();
						}
					}
				}
			}
		}

		public async Task<List<Playlist>> GetAllPlaylists()
		{
			List<Playlist> allplaylists = new List<Playlist>();
			var playlistsRequest = youtubeService.Playlists.List( "snippet" );
			playlistsRequest.Mine = true;
			playlistsRequest.MaxResults = 50;

			PlaylistListResponse playlistResponse = null;
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

		public async Task<List<PlaylistItem>> GetAllPlaylistItems( String playlistId )
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
					if( !allplaylistitems.Contains( plitem ) )
					{
						allplaylistitems.Add( plitem );
					}
				}
				playlistItemsRequest.PageToken = playlistItemListResponse.NextPageToken;
			}
			while( playlistItemListResponse.Items.Count > 0 && playlistItemListResponse.NextPageToken != null );

			return allplaylistitems;
		}

		public async Task CleanPlaylists()
		{
			try
			{
				var allplaylists = await GetAllPlaylists();
				foreach( var playlist in allplaylists )
				{
					var allvideos = await GetAllPlaylistItems( playlist.Id );
					foreach( var item in allvideos )
					{
						await youtubeService.PlaylistItems.Delete( item.Id ).ExecuteAsync();
					}
				}
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex );
			}
		}

		//go through every match that has a playlist id, then update the name of it to reflect the new one
		public async Task UpdatePlaylistsNames()
		{
			try
			{
				

				GlobalData globalData = await gameDatabase.GetGlobalData();
				
				foreach( String matchName in globalData.matches )
				{
					List<Task> matchTasks = new List<Task>();

					MatchData matchData = await gameDatabase.GetMatchData( matchName );

					if( matchData.youtubeUrl != null )
					{
						var playlistData = await GetPlaylistDataForMatch( matchName );
						playlistData.Id = matchData.youtubeUrl;
						matchTasks.Add( youtubeService.Playlists.Update( playlistData , "snippet,status" ).ExecuteAsync() );
					}

					foreach( String roundName in matchData.rounds )
					{
						//do the same for videos
						RoundData roundData = await gameDatabase.GetRoundData( roundName );
						if( roundData.youtubeUrl != null )
						{
							Video videoData = await GetVideoDataForRound( roundName );
							videoData.Id = roundData.youtubeUrl;
							matchTasks.Add( youtubeService.Videos.Update( videoData , "snippet,status,recordingDetails" ).ExecuteAsync() );
						}
					}

					await Task.WhenAll( matchTasks );
				}

				
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex );
			}
		}

		public async Task<Playlist> CreatePlaylist( String matchName , MatchData matchData )
		{
			try
			{
				Playlist pl = await GetPlaylistDataForMatch( matchName );
				var createPlaylistRequest = youtubeService.Playlists.Insert( pl , "snippet,status" );
				Playlist matchPlaylist = await createPlaylistRequest.ExecuteAsync();
				if( matchPlaylist != null )
				{
					matchData.youtubeUrl = matchPlaylist.Id;
				}

				return matchPlaylist;
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			return null;
		}

		public async Task AddRoundToPlaylist( String roundName , String matchName , List<PlaylistItem> playlistItems )
		{
			MatchData matchData = await gameDatabase.GetMatchData( matchName );
			RoundData roundData = await gameDatabase.GetRoundData( roundName );

			if( matchData.youtubeUrl == null || roundData.youtubeUrl == null )
			{
				Console.WriteLine( $"Could not add round {roundName} to playlist because either match url or round url are missing" );
				await Task.CompletedTask;
				return;
			}

			int roundIndex = matchData.rounds.IndexOf( roundName );

			if( !playlistItems.Any( x => x.Snippet.ResourceId.VideoId == roundData.youtubeUrl ) )
			{
				Console.WriteLine( $"Could not find {roundName} on playlist {matchName}, adding" );
				PlaylistItem roundPlaylistItem = await GetPlaylistItemForRound( roundName );
				roundPlaylistItem.Snippet.Position = roundIndex + 1;
				roundPlaylistItem.Snippet.PlaylistId = matchData.youtubeUrl;
				await youtubeService.PlaylistItems.Insert( roundPlaylistItem , "snippet" ).ExecuteAsync();
			}
		}

		public async Task UpdatePlaylists()
		{
			//TODO:I'm gonna regret writing this piece of shit tomorrow

			//go through every match, then try to find the playlist on youtube that contains its name, if it doesn't exist, create it
			//get all playlists first

			try
			{
				//queue all the addrounds tasks together
				List<Task> addrounds = new List<Task>();

				Console.WriteLine( "Searching all playlists..." );
				var allplaylists = await GetAllPlaylists();

				GlobalData globalData = await gameDatabase.GetGlobalData();

				foreach( var matchName in globalData.matches )
				{
					MatchData matchData = await gameDatabase.GetMatchData( matchName );

					//this returns the youtubeurl if it's not null otherwise it tries to search for it in all of the playlists title
					String matchPlaylist = String.IsNullOrEmpty( matchData.youtubeUrl )
						? allplaylists.FirstOrDefault( x => x.Snippet.Title.Contains( matchName ) )?.Id
						: matchData.youtubeUrl;

					try
					{
						if( String.IsNullOrEmpty( matchPlaylist ) )
						{
							Console.WriteLine( "Did not find playlist for {0}, creating" , matchName );
							//create the playlist now, doesn't matter that it's empty
							Playlist pl = await CreatePlaylist( matchName , matchData );
							if( pl != null )
							{
								await gameDatabase.SaveMatchData( matchName , matchData );
							}
						}
						else
						{
							//add this playlist id to the youtubeurl of the matchdata
							if( String.IsNullOrEmpty( matchData.youtubeUrl ) )
							{
								matchData.youtubeUrl = matchPlaylist;
								await gameDatabase.SaveMatchData( matchName , matchData );
							}
						}
					}
					catch( Exception ex )
					{
						Console.WriteLine( "Could not create playlist for {0}" , matchName , ex );
					}

					if( !String.IsNullOrEmpty( matchData.youtubeUrl ) )
					{
						List<PlaylistItem> playlistItems = await GetAllPlaylistItems( matchData.youtubeUrl );

						foreach( String roundName in matchData.rounds )
						{
							// 
							RoundData roundData = await gameDatabase.GetRoundData( roundName );
							if( roundData.youtubeUrl != null )
							{
								addrounds.Add( AddRoundToPlaylist( roundName , matchName , playlistItems ) );
							}
						}
					}
				}

				//finally await it all at once
				await Task.WhenAll( addrounds );
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex );
			}

			CommitGitChanges();
		}

		public async Task CleanupVideos()
		{
			GlobalData globalData = await gameDatabase.GetGlobalData();
			foreach( String roundName in globalData.rounds )
			{
				RoundData roundData = await gameDatabase.GetRoundData( roundName );
				if( roundData.youtubeUrl != null )
				{
					await RemoveVideoFile( roundName );
				}
			}
		}

		public void CommitGitChanges()
		{
			if( databaseRepository == null )
				return;

			Signature us = new Signature( Assembly.GetEntryAssembly().GetName().Name , uploaderSettings.gitEmail , DateTime.Now );
			var credentialsHandler = new CredentialsHandler(
				( url , usernameFromUrl , supportedCredentialTypes ) =>
					new UsernamePasswordCredentials()
					{
						Username = uploaderSettings.gitUsername ,
						Password = uploaderSettings.gitPassword ,
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

		public async Task<bool> UploadRoundToYoutubeAsync( String roundName )
		{
			if( youtubeService == null )
			{
				throw new NullReferenceException( "Youtube service is not initialized!!!" );
			}

			RoundData roundData = await gameDatabase.GetRoundData( roundName );

			if( roundData.youtubeUrl != null )
			{
				return false;
			}

			Video videoData = await GetVideoDataForRound( roundName );
			String filePath = gameDatabase.sharedSettings.GetRoundVideoPath( roundName );

			if( !File.Exists( filePath ) )
			{
				throw new ArgumentNullException( $"{roundName} does not contain a video!" );
			}

			using( var fileStream = new FileStream( filePath , FileMode.Open ) )
			{
				//get the pending upload for this roundName
				currentVideo = uploaderSettings.pendingUploads.Find( x => x.videoName.Equals( roundName ) );

				if( currentVideo == null )
				{
					currentVideo = new PendingUpload()
					{
						videoName = roundName
					};

					uploaderSettings.pendingUploads.Add( currentVideo );
				}

				currentVideo.fileSize = fileStream.Length;

				if( currentVideo.errorCount > uploaderSettings.retryCount )
				{
					currentVideo.uploadUrl = null;
					Console.WriteLine( "Replacing resumable upload url for {0} after too many errors" , currentVideo.videoName );
					currentVideo.errorCount = 0;
					currentVideo.lastException = String.Empty;
				}

				//TODO:Maybe it's possible to create a throttable request by extending the class of this one and initializing it with this one's values
				var videosInsertRequest = youtubeService.Videos.Insert( videoData , "snippet,status,recordingDetails" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videosInsertRequest.ProgressChanged += OnUploadProgress;
				videosInsertRequest.ResponseReceived += OnResponseReceived;
				videosInsertRequest.UploadSessionData += OnStartUploading;

				IUploadProgress uploadProgress = null;


				if( currentVideo.uploadUrl != null )
				{
					Console.WriteLine( "Resuming upload {0}" , currentVideo.videoName );
					uploadProgress = await videosInsertRequest.ResumeAsync( currentVideo.uploadUrl );
				}
				else
				{
					Console.WriteLine( "Beginning to upload {0}" , currentVideo.videoName );
					uploadProgress = await videosInsertRequest.UploadAsync();
				}

				//save it to the uploader settings and increment the error count only if it's not the annoying too many videos error
				if( uploadProgress.Status != UploadStatus.Completed
					&& uploadProgress.Exception is Google.GoogleApiException googleException
					&& googleException.Error?.Code != 400 )
				{
					currentVideo.lastException = uploadProgress.Exception.Message;
					currentVideo.errorCount++;
					SaveSettings();
				}
				currentVideo = null;
				return uploadProgress.Status == UploadStatus.Completed;
			}
		}

		private void OnStartUploading( IUploadSessionData resumable )
		{
			currentVideo.uploadUrl = resumable.UploadUri;
			SaveSettings();//save right away in case the program crashes or connection screws up
		}

		private void OnUploadProgress( IUploadProgress progress )
		{
			switch( progress.Status )
			{
				case UploadStatus.Uploading:
					{
						double percentage = Math.Round( ( (double) progress.BytesSent / (double) currentVideo.fileSize ) * 100f , 2 );
						//UpdateUploadProgress( percentage , true );
						Console.WriteLine( $"{currentVideo.videoName} : {percentage}%" );
						break;
					}
				case UploadStatus.Failed:
					Console.WriteLine( "An error prevented the upload from completing. {0}" , progress.Exception );
					break;
			}
		}

		private void OnResponseReceived( Video video )
		{
			if( uploaderSettings.pendingUploads.Contains( currentVideo ) )
			{
				uploaderSettings.pendingUploads.Remove( currentVideo );
			}

			SaveSettings();
			AddYoutubeIdToRound( currentVideo.videoName , video.Id ).Wait();

			Console.WriteLine( "Round {0} with id {1} was successfully uploaded." , currentVideo.videoName , video.Id );
		}

		private async Task RemoveVideoFile( string roundName )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName );

			//don't accidentally delete stuff that somehow doesn't have a url set
			if( roundData.youtubeUrl == null )
				return;

			try
			{
				String roundsFolder = Path.Combine( gameDatabase.sharedSettings.GetRecordingFolder() , gameDatabase.sharedSettings.roundsFolder );
				String filePath = Path.Combine( Path.Combine( roundsFolder , roundName ) , gameDatabase.sharedSettings.roundVideoFile );

				if( File.Exists( filePath ) )
				{
					Console.WriteLine( "Removed video file for {0}" , roundName );
					File.Delete( filePath );
				}
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}
		}


		private async Task UpdateUploadProgress( double percentage , bool updateInProgress = false )
		{
			if( discordClient == null )
				return;

			String gameString = String.Empty;
			if( updateInProgress )
			{
				gameString = $"Uploading {currentVideo.videoName} : {percentage}%";
			}
			else
			{
				gameString = $"{Math.Round( percentage )} videos remaining";
			}

			if( discordClient.CurrentUser.Presence.Game != null && discordClient.CurrentUser.Presence.Game.Name == gameString )
			{
				return;
			}

			await discordClient.UpdateStatusAsync( new DSharpPlus.Entities.DiscordGame( gameString ) );
		}

		private async Task<int> GetRemainingFilesCount()
		{
			int remainingFiles = 0;
			await gameDatabase.IterateOverAllRoundsOrMatches( false , async ( matchOrRound ) =>
			{
				IYoutube youtube = (IYoutube) matchOrRound;
				if( String.IsNullOrEmpty( youtube.youtubeUrl ) )
				{
					Interlocked.Increment( ref remainingFiles );
				}
				await Task.CompletedTask;
			} );

			return remainingFiles;
		}

		public async Task Run()
		{
			await LoadDatabase();
			await DoLogin();

			SaveSettings();
			CommitGitChanges();
			await UpdatePlaylists();
			await UploadAllRounds();
			
		}
	}
}
