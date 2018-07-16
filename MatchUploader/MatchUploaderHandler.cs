﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using MatchTracker;
using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;
using System.Threading;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Upload;
using System.Threading.Tasks;
using System.Reflection;
using System.Net.Http;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
/*
	Goes through all the folders, puts all rounds and matches into data.json
	Also returns match/round data from the timestamped name and whatnot
*/

namespace MatchUploader
{
	public class MatchUploaderHandler
	{
		private String settingsFolder;
		private GameDatabase gameDatabase;
		private UploaderSettings uploaderSettings;
		private YouTubeService youtubeService;
		private PendingUpload currentVideo;
		private Repository databaseRepository;
		private Branch currentBranch;

		public bool Initialized { get; }

		public MatchUploaderHandler()
		{
			gameDatabase = new GameDatabase();
			gameDatabase.LoadGlobalDataDelegate += LoadDatabaseGlobalDataFile;
			gameDatabase.LoadMatchDataDelegate += LoadDatabaseMatchDataFile;
			gameDatabase.LoadRoundDataDelegate += LoadDatabaseRoundDataFile;
			gameDatabase.SaveGlobalDataDelegate += SaveDatabaseGlobalDataFile;
			gameDatabase.SaveMatchDataDelegate += SaveDatabaseMatchDataFile;
			gameDatabase.SaveRoundDataDelegate += SaveDatabaseRoundataFile;

			Initialized = false;
			gameDatabase.sharedSettings = new SharedSettings();
			uploaderSettings = new UploaderSettings()
			{
				secrets = new ClientSecrets() ,
				dataStore = new KeyValueDataStore() ,
			};

			//load the settings
			//get the working directory
			settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );
			String uploaderSettingsPath = Path.Combine( settingsFolder , "uploader.json" );

			gameDatabase.sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
			uploaderSettings = JsonConvert.DeserializeObject<UploaderSettings>( File.ReadAllText( uploaderSettingsPath ) );
			Initialized = true;

			if( uploaderSettings.dataStore == null )
			{
				uploaderSettings.dataStore = new KeyValueDataStore();
			}

			if( Repository.IsValid( gameDatabase.sharedSettings.GetRecordingFolder() ) )
			{
				Console.WriteLine( "Loaded {0}" , gameDatabase.sharedSettings.GetRecordingFolder() );
				databaseRepository = new Repository( gameDatabase.sharedSettings.GetRecordingFolder() );
				currentBranch = databaseRepository.Branches.First( branch => branch.IsCurrentRepositoryHead );
			}
		}

		private async Task<MatchTracker.GlobalData> LoadDatabaseGlobalDataFile( SharedSettings sharedSettings )
		{
			await Task.CompletedTask;
			return sharedSettings.DeserializeGlobalData( File.ReadAllText( sharedSettings.GetGlobalPath() ) );
		}

		private async Task<MatchData> LoadDatabaseMatchDataFile( SharedSettings sharedSettings , string matchName )
		{
			await Task.CompletedTask;
			return sharedSettings.DeserializeMatchData( File.ReadAllText( sharedSettings.GetMatchPath( matchName ) ) );
		}

		private async Task<RoundData> LoadDatabaseRoundDataFile( SharedSettings sharedSettings , string roundName )
		{
			await Task.CompletedTask;
			return sharedSettings.DeserializeRoundData( File.ReadAllText( sharedSettings.GetRoundPath( roundName ) ) );
		}

		private async Task SaveDatabaseGlobalDataFile( SharedSettings sharedSettings , MatchTracker.GlobalData globalData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetGlobalPath() , sharedSettings.SerializeGlobalData( globalData ) );
		}

		private async Task SaveDatabaseMatchDataFile( SharedSettings sharedSettings , String matchName , MatchData matchData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetMatchPath( matchName ) , sharedSettings.SerializeMatchData( matchData ) );
		}

		private async Task SaveDatabaseRoundataFile( SharedSettings sharedSettings , String roundName , RoundData roundData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetRoundPath( roundName ) , sharedSettings.SerializeRoundData( roundData ) );
		}

		//in this context, settings are only the uploaderSettings
		public void SaveSettings()
		{
			File.WriteAllText(
				Path.Combine( settingsFolder , "uploader.json" ) ,
				JsonConvert.SerializeObject( uploaderSettings , Formatting.Indented )
			);
		}

		public async Task DoYoutubeLoginAsync()
		{
			UserCredential uc = null;

			var permissions = new [] { YouTubeService.Scope.Youtube };

			//TODO: allow switching between users? is this needed?

			uc = await GoogleWebAuthorizationBroker.AuthorizeAsync( uploaderSettings.secrets ,
				permissions ,
				"user" ,
				CancellationToken.None ,
				uploaderSettings.dataStore
			).ConfigureAwait( false );

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
			//

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
				globalData = gameDatabase.sharedSettings.GetGlobalData();
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

				RoundData roundData = await gameDatabase.GetRoundData( folderName ).ConfigureAwait( false );
				if( String.IsNullOrEmpty( roundData.name ) )
				{
					roundData.name = gameDatabase.sharedSettings.DateTimeToString( roundData.timeStarted );
					Console.WriteLine( $"Adding roundName to roundData {roundData.name}" );

					await gameDatabase.SaveRoundData( roundData.name , roundData ).ConfigureAwait( false );
				}
			}

			var matchFiles = Directory.EnumerateFiles( matchesPath );

			foreach( var matchPath in matchFiles )
			{
				String matchName = Path.GetFileNameWithoutExtension( matchPath );

				if( !globalData.matches.Contains( matchName ) )
				{
					globalData.matches.Add( matchName );
				}

				MatchData md = await gameDatabase.GetMatchData( matchName ).ConfigureAwait( false );

				//while we're here, let's check if all the players are added to the global data too
				foreach( PlayerData ply in md.players )
				{
					if( !globalData.players.Any( p => p.userId == ply.userId ) )
					{
						PlayerData toAdd = ply;
						ply.team = null;

						globalData.players.Add( toAdd );
					}
				}

				if( String.IsNullOrEmpty( md.name ) )
				{
					md.name = gameDatabase.sharedSettings.DateTimeToString( md.timeStarted );
					Console.WriteLine( $"Adding matchName to matchData {md.name}" );

					await gameDatabase.SaveMatchData( md.name , md ).ConfigureAwait( false );
				}
			}

			await gameDatabase.SaveGlobalData( globalData ).ConfigureAwait( false );
		}

		public async Task<Video> GetVideoDataForRound( String roundName )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );
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
					Title = roundName ,
					Tags = new List<String>() { "duckgame" , "peniscorp" } , //new string [] { "duckgame" } ,
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
			MatchData matchData = await gameDatabase.GetMatchData( matchName ).ConfigureAwait( false );

			String winner = matchData.GetWinnerName();

			if( String.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			return new Playlist()
			{
				Snippet = new PlaylistSnippet()
				{
					Title = matchName ,
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
			RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );
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
			RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );
			roundData.youtubeUrl = videoId;
			await gameDatabase.SaveRoundData( roundName , roundData ).ConfigureAwait( false );
		}

		public async Task UploadAllRounds()
		{
			Console.WriteLine( "Starting youtube uploads" );

			GlobalData globalData = await gameDatabase.GetGlobalData().ConfigureAwait( false );

			foreach( String matchName in globalData.matches )
			{
				MatchData matchData = await gameDatabase.GetMatchData( matchName ).ConfigureAwait( false );
				List<PlaylistItem> playlistItems = await GetAllPlaylistItems( matchData.youtubeUrl ).ConfigureAwait( false );

				foreach( String roundName in matchData.rounds )
				{
					RoundData oldRoundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );

					bool isUploaded = oldRoundData.youtubeUrl != null;

					if( !isUploaded )
					{
						await UploadRoundToYoutubeAsync( roundName ).ConfigureAwait( false );
						RoundData roundData = await gameDatabase.GetRoundData( roundName );
						await RemoveVideoFile( roundName );
						if( !isUploaded && roundData.youtubeUrl != null )
						{
							await AddRoundToPlaylist( roundName , matchName , playlistItems ).ConfigureAwait( false );
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
				playlistResponse = await playlistsRequest.ExecuteAsync().ConfigureAwait( false );

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
				playlistItemListResponse = await playlistItemsRequest.ExecuteAsync().ConfigureAwait( false );
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
				var allplaylists = await GetAllPlaylists().ConfigureAwait( false );
				foreach( var playlist in allplaylists )
				{
					var allvideos = await GetAllPlaylistItems( playlist.Id ).ConfigureAwait( false );
					foreach( var item in allvideos )
					{
						await youtubeService.PlaylistItems.Delete( item.Id ).ExecuteAsync().ConfigureAwait( false );
					}
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
				Playlist pl = await GetPlaylistDataForMatch( matchName ).ConfigureAwait( false );
				var createPlaylistRequest = youtubeService.Playlists.Insert( pl , "snippet,status" );
				Playlist matchPlaylist = await createPlaylistRequest.ExecuteAsync().ConfigureAwait( false );
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
			MatchData matchData = await gameDatabase.GetMatchData( matchName ).ConfigureAwait( false );
			RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );

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
				PlaylistItem roundPlaylistItem = await GetPlaylistItemForRound( roundName ).ConfigureAwait( false );
				roundPlaylistItem.Snippet.Position = roundIndex + 1;
				roundPlaylistItem.Snippet.PlaylistId = matchData.youtubeUrl;
				await youtubeService.PlaylistItems.Insert( roundPlaylistItem , "snippet" ).ExecuteAsync().ConfigureAwait( false );
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
				var allplaylists = await GetAllPlaylists().ConfigureAwait( false );

				GlobalData globalData = await gameDatabase.GetGlobalData().ConfigureAwait( false );

				foreach( var matchName in globalData.matches )
				{
					MatchData matchData = await gameDatabase.GetMatchData( matchName ).ConfigureAwait( false );

					//this returns the youtubeurl if it's not null otherwise it tries to search for it in all of the playlists title
					String matchPlaylist = String.IsNullOrEmpty( matchData.youtubeUrl )
						? allplaylists.FirstOrDefault( x => x.Snippet.Title == matchName )?.Id
						: matchData.youtubeUrl;

					try
					{
						if( String.IsNullOrEmpty( matchPlaylist ) )
						{
							Console.WriteLine( "Did not find playlist for {0}, creating" , matchName );
							//create the playlist now, doesn't matter that it's empty
							Playlist pl = await CreatePlaylist( matchName , matchData ).ConfigureAwait( false );
							if( pl != null )
							{
								await gameDatabase.SaveMatchData( matchName , matchData ).ConfigureAwait( false );
							}
						}
						else
						{
							//add this playlist id to the youtubeurl of the matchdata
							if( String.IsNullOrEmpty( matchData.youtubeUrl ) )
							{
								matchData.youtubeUrl = matchPlaylist;
								await gameDatabase.SaveMatchData( matchName , matchData ).ConfigureAwait( false );
							}
						}
					}
					catch( Exception ex )
					{
						Console.WriteLine( "Could not create playlist for {0}" , matchName , ex );
					}

					if( !String.IsNullOrEmpty( matchData.youtubeUrl ) )
					{
						List<PlaylistItem> playlistItems = await GetAllPlaylistItems( matchData.youtubeUrl ).ConfigureAwait( false );

						foreach( String roundName in matchData.rounds )
						{
							// 
							RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );
							if( roundData.youtubeUrl != null )
							{
								addrounds.Add( AddRoundToPlaylist( roundName , matchName , playlistItems ) );
							}
						}
					}
				}

				//finally await it all at once
				await Task.WhenAll( addrounds ).ConfigureAwait( false );
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex );
			}
		}

		public async Task CleanupVideos()
		{
			GlobalData globalData = await gameDatabase.GetGlobalData().ConfigureAwait( false );
			foreach( String roundName in globalData.rounds )
			{
				RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );
				if( roundData.youtubeUrl != null )
				{
					await RemoveVideoFile( roundName ).ConfigureAwait( false );
				}
			}
		}

		public void CommitGitChanges()
		{
			if( databaseRepository == null )
				return;

			Signature us = new Signature( Assembly.GetEntryAssembly().GetName().Name , uploaderSettings.gitEmail , DateTime.Now );
			bool hasChanges = false;

			Console.WriteLine( "Fetching repository status" );

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
					CredentialsProvider = new CredentialsHandler( ( url , usernameFromUrl , supportedCredentialTypes ) =>
					new UsernamePasswordCredentials()
					{
						Username = uploaderSettings.gitUsername ,
						Password = uploaderSettings.gitPassword ,
					} )
				};
				databaseRepository.Network.Push( currentBranch , pushOptions );
				Console.WriteLine( "Commit pushed" );
			}
		}

		public async Task UploadRoundToYoutubeAsync( String roundName )
		{
			if( youtubeService == null )
			{
				throw new NullReferenceException( "Youtube service is not initialized!!!" );
			}

			RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );

			if( roundData.youtubeUrl != null )
			{
				return;
			}

			Video videoData = await GetVideoDataForRound( roundName ).ConfigureAwait( false );

			String roundsFolder = Path.Combine( gameDatabase.sharedSettings.GetRecordingFolder() , gameDatabase.sharedSettings.roundsFolder );
			String filePath = Path.Combine( Path.Combine( roundsFolder , roundName ) , gameDatabase.sharedSettings.roundVideoFile );

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

				if( currentVideo.errorCount > uploaderSettings.retryCount )
				{
					currentVideo.uploadUrl = null;
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
					&& googleException.Error.Code != 400 )
				{
					currentVideo.lastException = uploadProgress.Exception.Message;
					currentVideo.errorCount++;
					SaveSettings();
				}
				currentVideo = null;
			}
		}

		private void OnStartUploading( IUploadSessionData resumable )
		{
			if( currentVideo.uploadUrl != null )
			{
				Console.WriteLine( "Replacing resumable upload url for {0}" , currentVideo.videoName );
				currentVideo.errorCount = 0;
				currentVideo.lastException = String.Empty;
			}
			currentVideo.uploadUrl = resumable.UploadUri;
			SaveSettings();//save right away in case the program crashes or connection screws up
		}

		private void OnUploadProgress( IUploadProgress progress )
		{
			switch( progress.Status )
			{
				case UploadStatus.Uploading:
					Console.WriteLine( "{0} bytes sent." , progress.BytesSent );
					break;

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
			SendVideoWebHook( video.Id );
		}

		private async Task RemoveVideoFile( string roundName )
		{
			RoundData roundData = await gameDatabase.GetRoundData( roundName ).ConfigureAwait( false );

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

		public void SendVideoWebHook( String videoId )
		{
			if( youtubeService == null || uploaderSettings.discordWebhook == null )
			{
				return;
			}

			Uri webhookUrl = uploaderSettings.discordWebhook;

			var message = new
			{
				content = String.Format( "https://www.youtube.com/watch?v={0}" , videoId ) ,
			};

			var content = new StringContent( JsonConvert.SerializeObject( message , Formatting.Indented ) , System.Text.Encoding.UTF8 , "application/json" );
			youtubeService.HttpClient.PostAsync( webhookUrl , content );
		}
	}
}
