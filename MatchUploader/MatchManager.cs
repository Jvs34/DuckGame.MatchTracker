using System;
using System.Collections.Generic;
using System.IO;
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

/*
	Goes through all the folders, puts all rounds and matches into data.json
	Also returns match/round data from the timestamped name and whatnot

*/
namespace MatchUploader
{
	public class MatchUploaderHandler
	{
		private SharedSettings sharedSettings;
		private MatchUploaderSettings settings;

		private String basePath;

		public MatchUploaderHandler()
		{
			BasePath = @"E:\DebugGameRecordings";
		}

		public MatchUploaderHandler( String path ) => BasePath = path;

		public string BasePath { get => basePath; set => basePath = value; }

		public async Task DoYoutubeLoginAsync()
		{
			UserCredential credential = null;
			
			using( var stream = new FileStream( @"C:\Users\Jvsth\OneDrive\Documents\DuckGame\Mods\MatchTracker\Settings\client_secrets.json" , FileMode.Open , FileAccess.Read ) )
			{
				var permissions = new [] { YouTubeService.Scope.YoutubeUpload };
				var secrets = GoogleClientSecrets.Load( stream ).Secrets;
				FileDataStore dataStore = new FileDataStore( @"C:\Users\Jvsth\OneDrive\Documents\DuckGame\Mods\MatchTracker\Settings" , true );
				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync( secrets , permissions , "user" , CancellationToken.None , dataStore );
				Console.WriteLine( dataStore.FolderPath + "\n" );
			}

			var youtubeService = new YouTubeService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential ,
				ApplicationName = "Duck Game Match Uploader" ,
			} );
		}


		//updates the global data.json
		public void UpdateGlobalData()
		{
			//

			String roundsPath = Path.Combine( BasePath , "rounds" );
			String matchesPath = Path.Combine( BasePath , "matches" );

			if( !Directory.Exists( BasePath ) || !Directory.Exists( roundsPath ) || !Directory.Exists( matchesPath ) )
			{
				throw new DirectoryNotFoundException( "Folders do not exist" );
			}

			String globalDataPath = Path.Combine( BasePath , "data.json" );



			GlobalData globalData = new GlobalData();

			if( File.Exists( globalDataPath ) )
			{
				String fileData = File.ReadAllText( globalDataPath );

				globalData = JsonConvert.DeserializeObject<GlobalData>( fileData );
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
				String matchName = Path.GetFileName( matchPath );
				Console.WriteLine( matchName + "\n" );
				if( !globalData.matches.Contains( matchName ) )
				{
					globalData.matches.Add( matchName );
				}
			}


			File.WriteAllText( globalDataPath , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}


		public async System.Threading.Tasks.Task UploadRoundToYoutubeAsync( String roundName )
		{
			UserCredential credential;

			/*
			using( var stream = new FileStream( @"C:\Users\Jvsth\OneDrive\Documents\DuckGame\Mods\MatchTracker\Settings\client_secrets.json" , FileMode.Open , FileAccess.Read ) )
			{
				var permissions = new [] { YouTubeService.Scope.YoutubeUpload };
				var secrets = GoogleClientSecrets.Load( stream ).Secrets;
				FileDataStore dataStore = new FileDataStore( @"C:\Users\Jvsth\OneDrive\Documents\DuckGame\Mods\MatchTracker\Settings" , true );
				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync( secrets , permissions , "user" , CancellationToken.None , dataStore );
				Console.WriteLine( dataStore.FolderPath + "\n" );
			}

			var youtubeService = new YouTubeService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential ,
				ApplicationName = "Duck Game Match Uploader" ,
			} );


			var video = new Video
			{
				Snippet = new VideoSnippet()
				{
					Title = "Duck Game Testing Video" ,
					Tags = new string [] { "duckgame" } ,
					CategoryId = "20" , // See https://developers.google.com/youtube/v3/docs/videoCategories/list
					Description = "This is a duck game recording" ,
				} ,
				Status = new VideoStatus()
				{
					PrivacyStatus = "unlisted" ,
				} ,
			};

			String roundsFolder = Path.Combine( BasePath , "rounds" );
			String filePath = Path.Combine( Path.Combine( roundsFolder , roundName ) , "video.mp4" );

			//ResumableUpload resumableUploadStream = new ResumableUpload();

			
			using( var fileStream = new FileStream( filePath , FileMode.Open ) )
			{
				var videosInsertRequest = youtubeService.Videos.Insert( video , "snippet,status" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = 64 * 1024;
				videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
				videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;
				videosInsertRequest.UploadSessionData += videoInserRequest_UploadSessionData;
				//await videosInsertRequest.UploadAsync();
			}
			*/
		}

		private void videoInserRequest_UploadSessionData( IUploadSessionData obj )
		{
			
		}

		void videosInsertRequest_ProgressChanged( IUploadProgress progress )
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

		void videosInsertRequest_ResponseReceived( Video video )
		{
			Console.WriteLine( "Video id '{0}' was successfully uploaded." , video.Id );
		}
	}
}
