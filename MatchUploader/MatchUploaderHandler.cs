using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using MatchTracker;
using Google.Apis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;
using System.Threading;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System.Threading.Tasks;
using System.Reflection;

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

		private bool initialized = false;
		private YouTubeService youtubeService;

		public bool Initialized
		{
			get => initialized;
		}

		public MatchUploaderHandler()
		{
			initialized = false;
			sharedSettings = new SharedSettings();
			uploaderSettings = new UploaderSettings()
			{
				secrets = new ClientSecrets() ,
				dataStore = new KeyValueDataStore() ,
			};

			//load the settings
			//since we're still debugging shit, we're running from visual studio
			settingsFolder = Path.Combine( Path.GetFullPath( Path.Combine( AppContext.BaseDirectory , "..\\..\\..\\..\\" ) ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );
			String uploaderSettingsPath = Path.Combine( settingsFolder , "uploader.json" );

			//try
			{
				sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
				uploaderSettings = JsonConvert.DeserializeObject<UploaderSettings>( File.ReadAllText( uploaderSettingsPath ) );
				initialized = true;

				if( uploaderSettings.dataStore == null )
				{
					uploaderSettings.dataStore = new KeyValueDataStore();
				}
			}
			/*
			catch( Exception e )
			{
				initialized = false;
			}
			*/

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

			var permissions = new [] { YouTubeService.Scope.YoutubeUpload };

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
				GZipEnabled = true,
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

			Console.WriteLine( "Rounds\n" );
			foreach( var folderPath in roundFolders )
			{
				//if it doesn't contain the folder, check if the round is valid
				String folderName = Path.GetFileName( folderPath );
				Console.WriteLine( folderName + "\n" );

				if( !globalData.rounds.Contains( folderName ) )
				{
					globalData.rounds.Add( folderName );
				}
			}

			var matchFiles = Directory.EnumerateFiles( matchesPath );

			Console.WriteLine( "Matches\n" );
			foreach( var matchPath in matchFiles )
			{
				String matchName = Path.GetFileNameWithoutExtension( matchPath );
				Console.WriteLine( matchName + "\n" );

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

						Console.WriteLine( "The global data does now contains " + ply.userId + "\n" );
					}
				}


			}


			sharedSettings.SaveGlobalData( globalData );

		}


		public Video GetVideoDataForRound( String roundName )
		{
			Video videoData = new Video()
			{
				Snippet = new VideoSnippet()
				{
					Title = roundName ,
					Tags = new List<String>() { "duckgame" } , //new string [] { "duckgame" } ,
					CategoryId = "20" , // See https://developers.google.com/youtube/v3/docs/videoCategories/list
					Description = "This is a duck game recording" ,
					
				} ,
				Status = new VideoStatus()
				{
					PrivacyStatus = "unlisted" ,
				} ,

			};

			return videoData;
		}

		public void AddYoutubeIdToRound( String roundName , String videoId )
		{

		}


		public async Task UploadRoundToYoutubeAsync( String roundName )
		{
			if( youtubeService == null )
			{
				throw new NullReferenceException( "Youtube service is not initialized!!!\n" );
			}

			Console.WriteLine( "Beginning to upload {0} \n" , roundName );

			Video videoData = GetVideoDataForRound( roundName );

			String roundsFolder = Path.Combine( sharedSettings.GetRecordingFolder() , sharedSettings.roundsFolder );
			String filePath = Path.Combine( Path.Combine( roundsFolder , roundName ) , sharedSettings.roundVideoFile );

			//is this a resumable one?

			bool resumeUpload = uploaderSettings.uploadToResume == roundName && uploaderSettings.uploadToResumeURI != null;
			uploaderSettings.uploadToResume = roundName;

			//await Task.Delay( TimeSpan.FromSeconds( 5 ) );

			using( var fileStream = new FileStream( filePath , FileMode.Open ) )
			{
				var videosInsertRequest = youtubeService.Videos.Insert( videoData , "snippet,status" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videosInsertRequest.ProgressChanged += OnUploadProgress;
				videosInsertRequest.ResponseReceived += OnResponseReceived;
				videosInsertRequest.UploadSessionData += OnStartUploading;
				//await videosInsertRequest
				if( resumeUpload )
				{
					Console.WriteLine( "Resuming upload\n" );
					await videosInsertRequest.ResumeAsync( uploaderSettings.uploadToResumeURI );
				}
				else
				{
					Console.WriteLine( "Starting a new upload\n" );
					await videosInsertRequest.UploadAsync();
				}
				
			}

		}


		private void OnStartUploading( IUploadSessionData resumable )
		{
			uploaderSettings.uploadToResumeURI = resumable.UploadUri;
			//save right away in case the program crashes or connection screws up
			SaveSettings();
		}

		void OnUploadProgress( IUploadProgress progress )
		{
			switch( progress.Status )
			{
				case UploadStatus.Uploading:
					Console.WriteLine( "{0} bytes sent." , progress.BytesSent );
					break;

				case UploadStatus.Failed:
					Console.WriteLine( "An error prevented the upload from completing.\n{0}" , progress.Exception );
					break;
			}
		}

		void OnResponseReceived( Video video )
		{
			String roundName = uploaderSettings.uploadToResume;
			uploaderSettings.uploadToResume = null;
			uploaderSettings.uploadToResumeURI = null;
			SaveSettings();

			AddYoutubeIdToRound( roundName , video.Id );

			Console.WriteLine( "Video id '{0}' was successfully uploaded." , video.Id );
		}
	}
}
