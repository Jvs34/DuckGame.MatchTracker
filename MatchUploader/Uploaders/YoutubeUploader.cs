using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MatchTracker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
					new [] {
						YouTubeService.Scope.Youtube
					} ,
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

			//check if there's any videos to upload singularly
			await DB.IterateOverAll<RoundData>( async ( roundData ) =>
			{
				await Task.CompletedTask;
				if( uploadableRounds.Count >= Info.UploadsBeforeReset )
				{
					return false;
				}

				if( roundData.RecordingType == RecordingType.Video && string.IsNullOrEmpty( roundData.YoutubeUrl ) )
				{
					//check if there's any video files too first
					FileInfo fileInfo = GetVideoFileInfo( roundData );

					if( fileInfo.Exists )
					{
						Console.WriteLine( $"Found upload to do {roundData.DatabaseIndex}" );
						uploadableRounds.Add( roundData );
					}
				}
				return true;
			} );

			foreach( var roundData in uploadableRounds.OrderBy( roundData => roundData.TimeStarted ) )
			{
				PendingUpload upload = await CreatePendingUpload( roundData );

				if( upload != null )
				{
					Uploads.Enqueue( upload );
				}
			}

		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			RoundData roundData = await DB.GetData<RoundData>( upload.DataName );

			if( !string.IsNullOrEmpty( roundData.YoutubeUrl ) )
			{
				Console.WriteLine( "Tried to upload a round that already had a youtube video on it somehow??" );
				return true;
			}

			Video videoData = await GetVideoDataForDatabaseItem( roundData );
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

			IUploadProgress uploadProgress;

			using( var fileStream = new FileStream( filePath , FileMode.Open ) )
			{
				Stream videoStream = fileStream;

				if( UploaderSettings.MaxKilobytesPerSecond > 0 )
				{
					videoStream = new ThrottledStream( fileStream , (int) ( 1024 * UploaderSettings.MaxKilobytesPerSecond ) );
				}

				if( upload.ErrorCount > UploaderSettings.RetryCount )
				{
					upload.UploadUrl = null;
					Console.WriteLine( "Replacing resumable upload url for {0} after too many errors" , upload.DataName );
					upload.ErrorCount = 0;
					upload.LastException = string.Empty;
				}

				//TODO:Maybe it's possible to create a throttable request by extending the class of this one and initializing it with this one's values
				var videosInsertRequest = Service.Videos.Insert( videoData , "snippet,status,recordingDetails" , videoStream , "video/*" );
				videosInsertRequest.ChunkSize = ResumableUpload.DefaultChunkSize;
				videosInsertRequest.ProgressChanged += OnYoutubeUploadProgress;
				videosInsertRequest.ResponseReceived += OnYoutubeUploadFinished;
				videosInsertRequest.UploadSessionData += OnYoutubeUploadStart;

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

				if( uploadProgress.Status != UploadStatus.Completed && upload.UploadUrl != null )
				{
					upload.LastException = uploadProgress.Exception.Message;
					SaveSettings();
				}

				videosInsertRequest.ProgressChanged -= OnYoutubeUploadProgress;
				videosInsertRequest.ResponseReceived -= OnYoutubeUploadFinished;
				videosInsertRequest.UploadSessionData -= OnYoutubeUploadStart;

				//fetch the data again if it was modified
				roundData = await DB.GetData<RoundData>( upload.DataName );
				if( !string.IsNullOrEmpty( roundData.YoutubeUrl ) )
				{
					//now add it to the playlist of the match
					await AddRoundToPlaylist( roundData.DatabaseIndex );
				}

				if( videoStream is ThrottledStream )
				{
					videoStream.Dispose();
					videoStream = null;
				}
			}

			if( uploadProgress.Exception != null )
			{
				throw uploadProgress.Exception;
			}

			await RemoveRoundVideoFile( roundData.DatabaseIndex );



			return uploadProgress.Status == UploadStatus.Completed;
		}

		#region UPLOADCALLBACKS
		private void OnYoutubeUploadStart( IUploadSessionData resumable )
		{
			CurrentUpload.UploadUrl = resumable.UploadUri;
			SaveSettings();
		}

		private void OnYoutubeUploadFinished( Video video )
		{
			SaveSettings();
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

						double percentage = Math.Round( progress.BytesSent / (double) CurrentUpload.FileSize * 100f , 2 );
						//UpdateUploadProgress( percentage , true );
						Console.WriteLine( $"{CurrentUpload.DataName} : {percentage}%" );
						break;
					}
				case UploadStatus.Failed:
					CurrentUpload.LastException = progress.Exception.ToString();
					break;
			}
		}
		#endregion

		private FileInfo GetVideoFileInfo( RoundData roundData )
		{
			FileInfo videoInfo = new FileInfo( DB.SharedSettings.GetRoundVideoPath( roundData.DatabaseIndex , false ) );

			FileInfo reencodedInfo = new FileInfo( Path.ChangeExtension( DB.SharedSettings.GetRoundVideoPath( roundData.DatabaseIndex , false ) , "converted.mp4" ) );

			if( reencodedInfo.Exists )
			{
				videoInfo = reencodedInfo;
			}

			return videoInfo;
		}

		private async Task<Video> GetVideoDataForDatabaseItem<T>( T data ) where T : IPlayersList, IStartEnd, IWinner, IDatabaseEntry
		{
			await Task.CompletedTask;

			var playerWinners = await DB.GetAllData<PlayerData>( data.GetWinners() );

			string winner = string.Join( " " , playerWinners.Select( x => x.GetName() ) );

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			string description = $"Recorded on {DB.SharedSettings.DateTimeToString( data.TimeStarted )}\nThe winner is {winner}";

			Video videoData = new Video()
			{
				Snippet = new VideoSnippet()
				{
					Title = $"{data.DatabaseIndex} {winner}" ,
					Tags = new List<string>() { "duckgame" , "peniscorp" } ,
					CategoryId = "20" ,
					Description = description ,
				} ,
				Status = new VideoStatus()
				{
					PrivacyStatus = "unlisted" ,
					MadeForKids = false ,
				} ,
				RecordingDetails = new VideoRecordingDetails()
				{
					RecordingDate = data.TimeStarted.ToString( "s" , CultureInfo.InvariantCulture ) ,
				}
			};

			return videoData;
		}

		private async Task RemoveRoundVideoFile( string roundName )
		{
			RoundData roundData = await DB.GetData<RoundData>( roundName );

			//don't accidentally delete stuff that somehow doesn't have a url set
			if( roundData.YoutubeUrl == null )
			{
				return;
			}

			try
			{
				string filePath = DB.SharedSettings.GetRoundVideoPath( roundName );
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

		private async Task AddYoutubeIdToRound( string roundName , string videoId )
		{
			RoundData roundData = await DB.GetData<RoundData>( roundName );
			roundData.YoutubeUrl = videoId;
			await DB.SaveData( roundData );
		}

		private async Task AddRoundToPlaylist( string roundName )
		{
			RoundData roundData = await DB.GetData<RoundData>( roundName );
			MatchData matchData = await DB.GetData<MatchData>( roundData.MatchName );

			if( matchData == null )
			{
				return;
			}

			if( string.IsNullOrEmpty( matchData.YoutubeUrl ) )
			{
				Console.WriteLine( $"Created playlist for {matchData.DatabaseIndex}" );
				await CreatePlaylist( roundData.MatchName );
			}

			matchData = await DB.GetData<MatchData>( roundData.MatchName );

			//wasn't created, still null
			if( string.IsNullOrEmpty( matchData.YoutubeUrl ) )
			{
				return;
			}

			await AddRoundToPlaylist( roundName , roundData.MatchName );
		}

		private async Task<Playlist> CreatePlaylist( string matchName )
		{
			Playlist matchPlaylist = null;

			try
			{
				MatchData matchData = await DB.GetData<MatchData>( matchName );
				matchPlaylist = await Service.Playlists.Insert( await GetPlaylistDataForMatch( matchData ) , "snippet,status" ).ExecuteAsync();

				if( matchPlaylist != null )
				{
					matchData.YoutubeUrl = matchPlaylist.Id;
					await DB.SaveData( matchData );
				}
			}
			catch( Exception e )
			{
				Console.WriteLine( $"Could not create playlist {matchName} , {e.Message}" );
			}

			return matchPlaylist;
		}

		private async Task AddRoundToPlaylist( string roundName , string matchName )
		{
			MatchData matchData = await DB.GetData<MatchData>( matchName );
			RoundData roundData = await DB.GetData<RoundData>( roundName );

			if( matchData.YoutubeUrl == null || roundData.YoutubeUrl == null )
			{
				Console.WriteLine( $"Could not add round {roundData.Name} to playlist because either match url or round url are missing" );
				await Task.CompletedTask;
				return;
			}

			int roundIndex = matchData.Rounds.IndexOf( roundData.Name );

			try
			{
				Console.WriteLine( $"Adding {roundData.Name} to playlist {matchData.Name}" );
				PlaylistItem roundPlaylistItem = await GetPlaylistItemForRound( roundData );
				roundPlaylistItem.Snippet.Position = roundIndex + 1;
				roundPlaylistItem.Snippet.PlaylistId = matchData.YoutubeUrl;
				await Service.PlaylistItems.Insert( roundPlaylistItem , "snippet" ).ExecuteAsync();
			}
			catch( Exception e )
			{
				Console.WriteLine( e.Message );
			}
		}

		private async Task<PlaylistItem> GetPlaylistItemForRound( RoundData roundData )
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

		private async Task<Playlist> GetPlaylistDataForMatch( MatchData matchData )
		{
			await Task.CompletedTask;

			var playerWinners = await DB.GetAllData<PlayerData>( matchData.GetWinners() );

			string winner = string.Join( " " , playerWinners.Select( x => x.GetName() ) );

			if( string.IsNullOrEmpty( winner ) )
			{
				winner = "Nobody";
			}

			return new Playlist()
			{
				Snippet = new PlaylistSnippet()
				{
					Title = $"{matchData.Name} {winner}" ,
					Description = string.Format( "Recorded on {0}\nThe winner is {1}" , DB.SharedSettings.DateTimeToString( matchData.TimeStarted ) , winner ) ,
					Tags = new List<string>() { "duckgame" , "peniscorp" }
				} ,
				Status = new PlaylistStatus()
				{
					PrivacyStatus = "public"
				}
			};
		}

		protected override async Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry )
		{
			await Task.CompletedTask;

			PendingUpload upload = null;

			if( entry is RoundData roundData )
			{
				FileInfo videoInfo = GetVideoFileInfo( roundData );

				if( videoInfo.Exists )
				{
					upload = new PendingUpload()
					{
						DataName = roundData.DatabaseIndex ,
						FileSize = videoInfo.Length ,
						DataType = nameof( RoundData )
					};
				}
			}

			return upload;
		}
	}
}
