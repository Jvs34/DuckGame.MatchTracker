using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MatchTracker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MatchUploader
{
	public class YoutubeUploader : Uploader
	{
		public YouTubeService Service { get; private set; }

		public YoutubeUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase , UploaderSettings settings ) : base( uploaderInfo , gameDatabase , settings )
		{
		}

		public override async Task Initialize()
		{
			string appName = GetType().Assembly.GetName().Name;
			Service = new YouTubeService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( UploaderSettings.Secrets ,
					new [] { YouTubeService.Scope.Youtube } ,
					"youtube" ,
					CancellationToken.None ,
					UploaderSettings.DataStore
				) ,
				ApplicationName = appName ,
				GZipEnabled = true ,
			} );
			Service.HttpClient.Timeout = TimeSpan.FromMinutes( 2 );
		}

		public override void CreateDefaultInfo()
		{
			Info.HasApiLimit = true;
			Info.NextReset = TimeSpan.FromHours( 24 );
			Info.Retries = 3;
			Info.UploadsBeforeReset = 100;//this might turn into 50 sometimes??verify
		}

		protected override async Task FetchUploads()
		{
			ConcurrentBag<RoundData> uploadableRounds = new ConcurrentBag<RoundData>();
			await DB.IterateOverAll<RoundData>( async ( roundData ) =>
			{
				await Task.CompletedTask;
				if( uploadableRounds.Count >= Info.UploadsBeforeReset )
				{
					return false;
				}

				if( roundData.RecordingType == RecordingType.Video && string.IsNullOrEmpty( roundData.YoutubeUrl ) )
				{
					uploadableRounds.Add( roundData );
				}
				return true;
			} );

			foreach( var roundData in uploadableRounds.OrderBy( roundData => roundData.TimeStarted ) )
			{
				PendingUpload upload = CreatePendingUpload( roundData );

				if( upload != null )
				{
					Uploads.Enqueue( upload );
				}
			}

		}

		private async Task<Video> GetVideoDataForRound( RoundData roundData )
		{
			await Task.CompletedTask;

			var playerWinners = await DB.GetAllData<PlayerData>( roundData.GetWinners().ToArray() );

			string winner = string.Join( " " , playerWinners.Select( x => x.GetName() ) );

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			string description = $"Recorded on {DB.SharedSettings.DateTimeToString( roundData.TimeStarted )}\nThe winner is {winner}";

			Video videoData = new Video()
			{
				Snippet = new VideoSnippet()
				{
					Title = $"{roundData.Name} {winner}" ,
					Tags = new List<string>() { "duckgame" , "peniscorp" } ,
					CategoryId = "20" ,
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

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			RoundData roundData = await DB.GetData<RoundData>( upload.DataName );

			Video videoData = await GetVideoDataForRound( roundData );
			string filePath = DB.SharedSettings.GetRoundVideoPath( roundData.Name );

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
				if( upload.ErrorCount > UploaderSettings.RetryCount )
				{
					upload.UploadUrl = null;
					Console.WriteLine( "Replacing resumable upload url for {0} after too many errors" , upload.DataName );
					upload.ErrorCount = 0;
					upload.LastException = string.Empty;
				}

				//TODO:Maybe it's possible to create a throttable request by extending the class of this one and initializing it with this one's values
				var videosInsertRequest = Service.Videos.Insert( videoData , "snippet,status,recordingDetails" , fileStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videosInsertRequest.ProgressChanged += OnYoutubeUploadProgress;
				videosInsertRequest.ResponseReceived += OnYoutubeUploadFinished;
				videosInsertRequest.UploadSessionData += OnYoutubeUploadStart;

				IUploadProgress uploadProgress;

				if( upload.UploadUrl != null )
				{
					Console.WriteLine( "Resuming upload {0}" , upload.DataName );
					uploadProgress = await videosInsertRequest.ResumeAsync( upload.UploadUrl );
				}
				else
				{
					Console.WriteLine( "Beginning to upload {0}" , upload.DataName );
					uploadProgress = await videosInsertRequest.UploadAsync();
				}

				//save it to the uploader settings and increment the error count only if it's not the annoying too many videos error
				if( uploadProgress.Status != UploadStatus.Completed && upload.UploadUrl != null )
				{
					upload.LastException = uploadProgress.Exception.Message;
					upload.ErrorCount++;
					await SaveSettings();
				}

				videosInsertRequest.ProgressChanged -= OnYoutubeUploadProgress;
				videosInsertRequest.ResponseReceived -= OnYoutubeUploadFinished;
				videosInsertRequest.UploadSessionData -= OnYoutubeUploadStart;

				return uploadProgress.Status == UploadStatus.Completed;
			}
		}

		private void OnYoutubeUploadStart( IUploadSessionData resumable )
		{
			CurrentUpload.UploadUrl = resumable.UploadUri;
			SaveSettings().Wait();
		}

		private async Task AddYoutubeIdToRound( string roundName , string videoId )
		{
			RoundData roundData = await DB.GetData<RoundData>( roundName );
			roundData.YoutubeUrl = videoId;
			await DB.SaveData( roundData );
		}

		private void OnYoutubeUploadFinished( Video video )
		{
			SaveSettings().Wait();
			AddYoutubeIdToRound( CurrentUpload.DataName , video.Id ).Wait();

			Console.WriteLine( "Round {0} with id {1} was successfully uploaded." , CurrentUpload.DataName , video.Id );
		}

		private void OnYoutubeUploadProgress( IUploadProgress progress )
		{
			switch( progress.Status )
			{
				case UploadStatus.Uploading:
					{
						CurrentUpload.BytesSent = progress.BytesSent;

						double percentage = Math.Round( ( (double) progress.BytesSent / (double) CurrentUpload.FileSize ) * 100f , 2 );
						//UpdateUploadProgress( percentage , true );
						Console.WriteLine( $"{CurrentUpload.DataName} : {percentage}%" );
						break;
					}
				case UploadStatus.Failed:
					CurrentUpload.LastException = progress.Exception.ToString();
					break;
			}
		}
	}
}
