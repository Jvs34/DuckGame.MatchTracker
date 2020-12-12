using Humanizer.Bytes;
using MatchTracker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace MatchUploader
{
	class MatchMerger : Uploader, IProgress<ProgressInfo>
	{
		public MatchMerger( IGameDatabase gameDatabase , UploaderSettings settings ) : base( gameDatabase , settings )
		{
		}

		public override async Task Initialize()
		{
			//download ffmpeg here
			string tempFFmpegFolder = Path.Combine( Path.GetTempPath() , "ffmpeg" );

			if( !Directory.Exists( tempFFmpegFolder ) )
			{
				Directory.CreateDirectory( tempFFmpegFolder );
				Console.WriteLine( $"Created directory {tempFFmpegFolder}" );
			}

			FFmpeg.SetExecutablesPath( tempFFmpegFolder );

			await FFmpegDownloader.GetLatestVersion( FFmpegVersion.Official , FFmpeg.ExecutablesPath , this );
		}

		public override void SetupDefaultInfo()
		{
			Info.HasApiLimit = false;
			Info.Retries = 0;
		}

		public void Report( ProgressInfo value )
		{
			ByteSize byteSize = ByteSize.FromBytes( value.DownloadedBytes );
			float prgr = (float) value.DownloadedBytes / (float) value.TotalBytes * 100f;
			Console.WriteLine( $"Downloaded:  {(int) prgr}% {byteSize.ToFullWords()}" );
		}

		protected override async Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry )
		{
			await Task.CompletedTask;

			PendingUpload upload = null;

			if( entry is MatchData matchData )
			{
				upload = new PendingUpload()
				{
					DataName = matchData.DatabaseIndex ,
					FileSize = 0 ,
					DataType = nameof( MatchData )
				};
			}

			return upload;
		}

		private async Task<List<string>> GetEligibleMatchNames()
		{
			var concMatches = new ConcurrentBag<MatchData>();

			await DB.IterateOverAll<MatchData>( async ( matchData ) =>
			{
				bool suitable = matchData.VideoType == VideoType.PlaylistLink;

				if( suitable )
				{
					foreach( var roundName in matchData.Rounds )
					{
						RoundData roundData = await DB.GetData<RoundData>( roundName );

						if( !string.IsNullOrEmpty( roundData.YoutubeUrl ) || !File.Exists( DB.SharedSettings.GetRoundVideoPath( roundName ) ) )
						{
							suitable = false;
							break;
						}
					}
				}

				if( suitable )
				{
					concMatches.Add( matchData );
				}
				return true;
			} );

			return concMatches.OrderBy( x => x.TimeStarted ).Select( x => x.DatabaseIndex ).ToList();
		}

		protected override async Task FetchUploads()
		{
			var matches = await GetEligibleMatchNames();

			foreach( var matchName in matches )
			{
				var upload = await CreatePendingUpload( await DB.GetData<MatchData>( matchName ) );
				if( upload != null )
				{
					Uploads.Enqueue( upload );
				}
			}
		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			var videoFileName = DB.SharedSettings.GetMatchVideoPath( upload.DataName );
			var videoFileTempName = Path.ChangeExtension( DB.SharedSettings.GetMatchVideoPath( upload.DataName ) , "temp" );

			bool success = false;

			if( File.Exists( videoFileName ) )
			{
				return true;
			}

			try
			{
				var matchData = await DB.GetData<MatchData>( upload.DataName );
				var conversion = await FFmpeg.Conversions.FromSnippet.Concatenate(
					videoFileTempName ,
					matchData.Rounds.Select( x => DB.SharedSettings.GetRoundVideoPath( x ) ).ToArray()
				);

				await conversion.Start();

				success = true;
			}
			catch( Exception e )
			{
				success = false;
			}

			if( success )
			{
				File.Move( videoFileTempName , videoFileName );
				//TODO: delete all the video files of the rounds, then set their type as mergedvideolink
				//and set their respective timespan times of when they appear in the videos

				var matchData = await DB.GetData<MatchData>( upload.DataName );
				//matchData.VideoType = VideoType.MergedVideoLink;
				await DB.SaveData( matchData );

				/*
				foreach( var roundName in matchData.Rounds )
				{
					var roundVideoFile = DB.SharedSettings.GetRoundVideoPath( roundName );
					if( File.Exists( roundVideoFile ) )
					{
						File.Delete( roundVideoFile );
					}
				}
				*/

				/*
				await DB.IterateOver<RoundData>( async ( roundData ) =>
				{
					roundData.VideoType = VideoType.MergedVideoLink;
					await DB.SaveData( roundData );
					return true;
				} , matchData.Rounds );
				*/

			}
			else
			{
				File.Delete( videoFileTempName );
			}

			return success;
		}
	}
}
