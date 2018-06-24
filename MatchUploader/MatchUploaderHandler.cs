using System;
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
		private SharedSettings sharedSettings;
		private UploaderSettings uploaderSettings;
		private YouTubeService youtubeService;
		private string currentVideo;
		private Repository databaseRepository;
		private Branch currentBranch;

		public bool Initialized { get; }

		public MatchUploaderHandler()
		{
			Initialized = false;
			sharedSettings = new SharedSettings();
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

			sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
			uploaderSettings = JsonConvert.DeserializeObject<UploaderSettings>( File.ReadAllText( uploaderSettingsPath ) );
			Initialized = true;

			if( uploaderSettings.dataStore == null )
			{
				uploaderSettings.dataStore = new KeyValueDataStore();
			}


			if( Repository.IsValid( sharedSettings.GetRecordingFolder() ) )
			{
				Console.WriteLine( "Loaded {0}" , sharedSettings.GetRecordingFolder() );
				databaseRepository = new Repository( sharedSettings.GetRecordingFolder() );
				currentBranch = databaseRepository.Branches.First( branch => branch.IsCurrentRepositoryHead );

			}

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
		public void UpdateGlobalData()
		{
			//


			String roundsPath = Path.Combine( sharedSettings.GetRecordingFolder() , sharedSettings.roundsFolder );
			String matchesPath = Path.Combine( sharedSettings.GetRecordingFolder() , sharedSettings.matchesFolder );

			if( !Directory.Exists( sharedSettings.GetRecordingFolder() ) || !Directory.Exists( roundsPath ) || !Directory.Exists( matchesPath ) )
			{
				throw new DirectoryNotFoundException( "Folders do not exist" );
			}

			String globalDataPath = sharedSettings.GetGlobalPath();

			GlobalData globalData = new GlobalData();

			if( File.Exists( globalDataPath ) )
			{
				globalData = sharedSettings.GetGlobalData();
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

				RoundData roundData = sharedSettings.GetRoundData( folderName );
				if( String.IsNullOrEmpty( roundData.name ) )
				{
					roundData.name = sharedSettings.DateTimeToString( roundData.timeStarted );
					Console.WriteLine( $"Adding roundName to roundData {roundData.name}" );

					sharedSettings.SaveRoundData( roundData.name , roundData );
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

				MatchData md = sharedSettings.GetMatchData( matchName );

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
					md.name = sharedSettings.DateTimeToString( md.timeStarted );
					Console.WriteLine( $"Adding matchName to matchData {md.name}" );

					sharedSettings.SaveMatchData( md.name , md );
				}
			}

			sharedSettings.SaveGlobalData( globalData );

		}


		public Video GetVideoDataForRound( String roundName )
		{
			RoundData roundData = sharedSettings.GetRoundData( roundName );
			String winner = roundData.GetWinnerName();

			if( String.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}


			String description = String.Format( "Recorded on {0}\nThe winner is {1}" , sharedSettings.DateTimeToString( roundData.timeStarted ) , winner );


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

		public Playlist GetPlaylistDataForMatch( String matchName )
		{
			MatchData matchData = sharedSettings.GetMatchData( matchName );

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
					Description = String.Format( "Recorded on {0}\nThe winner is {1}" , sharedSettings.DateTimeToString( matchData.timeStarted ) , winner ) ,
					Tags = new List<String>() { "duckgame" , "peniscorp" }
				} ,
				Status = new PlaylistStatus()
				{
					PrivacyStatus = "public"
				}
			};
		}

		public PlaylistItem GetPlaylistItemForRound( String roundName )
		{
			RoundData roundData = sharedSettings.GetRoundData( roundName );
			return new PlaylistItem()
			{
				Snippet = new PlaylistItemSnippet()
				{
					ResourceId = new ResourceId()
					{
						Kind = "youtube#video",
						VideoId = roundData.youtubeUrl
					}
				},
			};
		}

		private void AddYoutubeIdToRound( String roundName , String videoId )
		{
			RoundData roundData = sharedSettings.GetRoundData( roundName );
			roundData.youtubeUrl = videoId;
			sharedSettings.SaveRoundData( roundName , roundData );
		}

		public async Task UploadAllRounds()
		{
			GlobalData globalData = sharedSettings.GetGlobalData();

			foreach( String roundName in globalData.rounds )
			{
				RoundData oldRoundData = sharedSettings.GetRoundData( roundName );
				await UploadRoundToYoutubeAsync( roundName ).ConfigureAwait( false );
				RoundData roundData = sharedSettings.GetRoundData( roundName );
				RemoveVideoFile( roundName );
				if( oldRoundData.youtubeUrl != roundData.youtubeUrl )
				{
					CommitGitChanges();
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

			}
		}

		public async Task UpdatePlaylists()
		{

			//TODO:I'm gonna regret writing this piece of shit tomorrow

			//go through every match, then try to find the playlist on youtube that contains its name, if it doesn't exist, create it
			//get all playlists first

			try
			{
				var allplaylists = await GetAllPlaylists();

				var playlistItemsRequest = youtubeService.PlaylistItems.List( "snippet" );

				GlobalData globalData = sharedSettings.GetGlobalData();
				foreach( var matchName in globalData.matches )
				{
					MatchData matchData = sharedSettings.GetMatchData( matchName );
					Playlist matchPlaylist = allplaylists.FirstOrDefault( x => x.Snippet.Title == matchName );

					try
					{
						if( matchPlaylist == null )
						{
							Console.WriteLine( "Did not find playlist for {0}, creating" , matchName );
							//create the playlist now, doesn't matter that it's empty
							Playlist pl = GetPlaylistDataForMatch( matchName );
							var createPlaylistRequest = youtubeService.Playlists.Insert( pl , "snippet,status" );
							matchPlaylist = await createPlaylistRequest.ExecuteAsync();
						}
					}
					catch( Exception ex )
					{
						Console.WriteLine( "Could not create playlist for {0}" , matchName );
					}

					//if we have a playlist now, add the rounds we already have avaiable to it
					if( matchPlaylist != null )
					{
						playlistItemsRequest.PlaylistId = matchPlaylist.Id;
						//get the playlist items now
						var playlistItems = await GetAllPlaylistItems( matchPlaylist.Id );

						for( int i = 0; i < matchData.rounds.Count; i++ )
						//foreach( String roundName in matchData.rounds )
						{
							String roundName = matchData.rounds [i];

							RoundData roundData = sharedSettings.GetRoundData( roundName );
							if( roundData.youtubeUrl != null )
							{
								//check if this youtube id is in the playlist, otherwise add it

								if( !playlistItems.Any( x => x.Snippet.ResourceId.VideoId == roundData.youtubeUrl ) )
								{
									Console.WriteLine( "Could not find video for playlist, adding" );
									PlaylistItem roundPlaylistItem = GetPlaylistItemForRound( roundName );
									roundPlaylistItem.Snippet.Position = i + 1;
									roundPlaylistItem.Snippet.PlaylistId = matchPlaylist.Id;
									await youtubeService.PlaylistItems.Insert( roundPlaylistItem , "snippet" ).ExecuteAsync();
								}
							}
						}
					}
				}
			}
			catch( Exception ex )
			{
			}

		}

		public void CleanupVideos()
		{
			GlobalData globalData = sharedSettings.GetGlobalData();
			foreach( String roundName in globalData.rounds )
			{
				RoundData roundData = sharedSettings.GetRoundData( roundName );
				if( roundData.youtubeUrl != null )
				{
					RemoveVideoFile( roundName );
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
					CredentialsProvider = new CredentialsHandler( ( url , usernameFromUrl , types ) =>
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

			RoundData roundData = sharedSettings.GetRoundData( roundName );

			if( roundData.youtubeUrl != null )
			{
				return;
			}

			Video videoData = GetVideoDataForRound( roundName );

			String roundsFolder = Path.Combine( sharedSettings.GetRecordingFolder() , sharedSettings.roundsFolder );
			String filePath = Path.Combine( Path.Combine( roundsFolder , roundName ) , sharedSettings.roundVideoFile );


			using( var fileStream = new FileStream( filePath , FileMode.Open ) )
			{
				currentVideo = roundName;
				//TODO:Maybe it's possible to create a throttable request by extending the class of this one and initializing it with this one's values
				var videosInsertRequest = youtubeService.Videos.Insert( videoData , "snippet,status,recordingDetails" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videosInsertRequest.ProgressChanged += OnUploadProgress;
				videosInsertRequest.ResponseReceived += OnResponseReceived;
				videosInsertRequest.UploadSessionData += OnStartUploading;

				if( uploaderSettings.pendingUploads.ContainsKey( currentVideo ) )
				{
					Console.WriteLine( "Resuming upload {0}" , currentVideo );
					if( uploaderSettings.pendingUploads.TryGetValue( currentVideo , out Uri resumableUri ) )
					{
						await videosInsertRequest.ResumeAsync( resumableUri );
					}
				}
				else
				{
					Console.WriteLine( "Beginning to upload {0}" , currentVideo );
					await videosInsertRequest.UploadAsync();
				}
				currentVideo = null;

			}
		}


		private void OnStartUploading( IUploadSessionData resumable )
		{

			if( uploaderSettings.pendingUploads.TryGetValue( currentVideo , out Uri resumableUri ) )
			{
				Console.WriteLine( "Replacing resumable upload url for {0}" , currentVideo );
				uploaderSettings.pendingUploads.Remove( currentVideo );
			}
			uploaderSettings.pendingUploads.Add( currentVideo , resumable.UploadUri );
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
			String roundName = currentVideo;
			uploaderSettings.pendingUploads.Remove( roundName );

			SaveSettings();
			AddYoutubeIdToRound( roundName , video.Id );

			Console.WriteLine( "Round {0} with id {1} was successfully uploaded." , currentVideo , video.Id );
			SendVideoWebHook( video.Id );
		}

		private void RemoveVideoFile( string roundName )
		{
			RoundData roundData = sharedSettings.GetRoundData( roundName );

			//don't accidentally delete stuff that somehow doesn't have a url set
			if( roundData.youtubeUrl == null )
				return;

			try
			{
				String roundsFolder = Path.Combine( sharedSettings.GetRecordingFolder() , sharedSettings.roundsFolder );
				String filePath = Path.Combine( Path.Combine( roundsFolder , roundName ) , sharedSettings.roundVideoFile );

				if( File.Exists( filePath ) )
				{
					Console.WriteLine( "Removed video file for {0}" , roundName );
					File.Delete( filePath );
				}
			}
			catch( Exception e )
			{

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
