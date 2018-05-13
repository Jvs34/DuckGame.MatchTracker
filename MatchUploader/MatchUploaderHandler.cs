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

			//try
			{
				sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
				uploaderSettings = JsonConvert.DeserializeObject<UploaderSettings>( File.ReadAllText( uploaderSettingsPath ) );
				Initialized = true;

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
			}

			sharedSettings.SaveGlobalData( globalData );

		}


		public Video GetVideoDataForRound( String roundName )
		{
			RoundData roundData = sharedSettings.GetRoundData( roundName );
			String winner = sharedSettings.GetRoundWinnerName( roundData );

			if( winner.Length == 0 )
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
			};

			return videoData;
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

			bool resumeUpload = uploaderSettings.uploadToResume != null && uploaderSettings.uploadToResumeURI != null;
			if( resumeUpload )
			{
				await UploadRoundToYoutubeAsync( uploaderSettings.uploadToResume ).ConfigureAwait( false );
			}

			//after that is done, start uploading everything else

			foreach( String roundName in globalData.rounds )
			{
				CommitGitChanges();
				await UploadRoundToYoutubeAsync( roundName ).ConfigureAwait( false );
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
			if( !Repository.IsValid( sharedSettings.GetRecordingFolder() ) )
			{
				Console.WriteLine( "{0} is not a valid git repository\n" );
				return;
			}

			using( Repository repository = new Repository( sharedSettings.GetRecordingFolder() ) )
			{
				Signature us = new Signature( Assembly.GetEntryAssembly().GetName().Name , uploaderSettings.gitEmail , DateTime.Now );

				Branch currentBranch = repository.Branches.First( branch => branch.IsCurrentRepositoryHead );

				if( currentBranch == null )
				{
					throw new NullReferenceException( "Branch is NULL???????" );
				}

				bool hasChanges = false;

				foreach( var item in repository.RetrieveStatus() )
				{
					if( item.State != FileStatus.Ignored && item.State != FileStatus.Unaltered )
					{
						Console.WriteLine( "File {0} {1}" , item.FilePath , item.State );

						Commands.Stage( repository , item.FilePath );
						hasChanges = true;
					}
				}


				if( hasChanges )
				{
					//Commands.Stage( repository , "*" );

					repository.Commit( "Updated database" , us , us );

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
					repository.Network.Push( currentBranch , pushOptions );
				}

				//await Task.Delay( 1000 );

				return;
			}
		}

		public async Task UploadRoundToYoutubeAsync( String roundName )
		{
			if( youtubeService == null )
			{
				throw new NullReferenceException( "Youtube service is not initialized!!!\n" );
			}

			RoundData roundData = sharedSettings.GetRoundData( roundName );

			if( roundData.youtubeUrl != null )
			{
				return;
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
				//TODO:Maybe it's possible to create a throttable request by extending the class of this one and initializing it with this one's values
				var videosInsertRequest = youtubeService.Videos.Insert( videoData , "snippet,status" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videosInsertRequest.ProgressChanged += OnUploadProgress;
				videosInsertRequest.ResponseReceived += OnResponseReceived;
				videosInsertRequest.UploadSessionData += OnStartUploading;

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
			SendVideoWebHook( video.Id );
			//RemoveVideoFile( roundName );
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
					Console.WriteLine( "Removed video file for {0}\n" , roundName );
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
