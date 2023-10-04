
//namespace MatchUploader
//{
//	class MatchMerger : Uploader, IProgress<ProgressInfo>
//	{
//		public MatchMerger( IGameDatabase gameDatabase , UploaderSettings settings ) : base( gameDatabase , settings )
//		{
//		}

//		public override async Task Initialize()
//		{
//			//download ffmpeg here
//			string tempFFmpegFolder = Path.Combine( Path.GetTempPath() , "ffmpeg" );

//			if( !Directory.Exists( tempFFmpegFolder ) )
//			{
//				Directory.CreateDirectory( tempFFmpegFolder );
//				Console.WriteLine( $"Created directory {tempFFmpegFolder}" );
//			}

//			FFmpeg.SetExecutablesPath( tempFFmpegFolder );

//			await FFmpegDownloader.GetLatestVersion( FFmpegVersion.Official , FFmpeg.ExecutablesPath , this );
//		}

//		public override void SetupDefaultInfo()
//		{
//			Info.HasApiLimit = false;
//			Info.Retries = 0;
//		}

//		public void Report( ProgressInfo value )
//		{
//			ByteSize byteSize = ByteSize.FromBytes( value.DownloadedBytes );
//			float prgr = (float) value.DownloadedBytes / (float) value.TotalBytes * 100f;
//			Console.WriteLine( $"Downloaded:  {(int) prgr}% {byteSize.ToFullWords()}" );
//		}

//		protected override async Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry )
//		{
//			await Task.CompletedTask;

//			PendingUpload upload = null;

//			if( entry is MatchData matchData )
//			{
//				upload = new PendingUpload()
//				{
//					DataName = matchData.DatabaseIndex ,
//					FileSize = 0 ,
//					DataType = nameof( MatchData )
//				};
//			}

//			return upload;
//		}

//		private async Task<List<string>> GetEligibleMatchNames()
//		{
//			var concMatches = new ConcurrentBag<MatchData>();

//			await DB.IterateOverAll<MatchData>( async ( matchData ) =>
//			{
//				bool suitable = matchData.VideoType == VideoUrlType.PlaylistLink && string.IsNullOrEmpty( matchData.YoutubeUrl ) && !File.Exists( DB.SharedSettings.GetMatchVideoPath( matchData.Name ) ) && matchData.Rounds.Count > 1;

//				if( suitable )
//				{
//					foreach( var roundName in matchData.Rounds )
//					{
//						RoundData roundData = await DB.GetData<RoundData>( roundName );

//						if( roundData.RecordingType != RecordingType.Video || !string.IsNullOrEmpty( roundData.YoutubeUrl ) || !File.Exists( DB.SharedSettings.GetRoundVideoPath( roundName ) ) )
//						{
//							suitable = false;
//							break;
//						}
//					}
//				}

//				if( suitable )
//				{
//					concMatches.Add( matchData );
//				}
//				return true;
//			} );

//			return concMatches.OrderBy( x => x.TimeStarted ).Select( x => x.DatabaseIndex ).ToList();
//		}

//		protected override async Task FetchUploads()
//		{
//			var matches = await GetEligibleMatchNames();

//			foreach( var matchName in matches )
//			{
//				var upload = await CreatePendingUpload( await DB.GetData<MatchData>( matchName ) );
//				if( upload != null )
//				{
//					Uploads.Enqueue( upload );
//				}
//			}
//		}

//		protected override async Task<bool> UploadItem( PendingUpload upload )
//		{
//			var videoFileName = DB.SharedSettings.GetMatchVideoPath( upload.DataName );
//			var videoFileTempName = Path.ChangeExtension( DB.SharedSettings.GetMatchVideoPath( upload.DataName ) , ".temp.mp4" );

//			bool success = false;

//			if( File.Exists( videoFileName ) )
//			{
//				return true;
//			}

//			if( File.Exists( videoFileTempName ) )
//			{
//				File.Delete( videoFileTempName );
//			}

//			try
//			{
//				var matchData = await DB.GetData<MatchData>( upload.DataName );
//				var conversion = await FFmpeg.Conversions.FromSnippet.Concatenate(
//					videoFileTempName ,
//					matchData.Rounds.Select( roundName => DB.SharedSettings.GetRoundVideoPath( roundName ) ).ToArray()
//				);
//				Console.WriteLine( $"Starting conversion of {matchData.Name} with {matchData.Rounds.Count} rounds" );

//				var stopwatch = new System.Diagnostics.Stopwatch();

//				stopwatch.Start();


//				await conversion.Start();

//				stopwatch.Stop();

//				Console.WriteLine( $"Conversion of {matchData.Name} took {stopwatch.Elapsed.Humanize()}" );

//				success = true;
//			}
//			catch( Exception e )
//			{
//				Console.WriteLine( e );
//				success = false;
//			}

//			if( success )
//			{
//				Console.WriteLine( "Applying changes and removing leftover videos..." );

//				File.Move( videoFileTempName , videoFileName );

//				// delete all the video files of the rounds, then set their type as mergedvideolink
//				//and set their respective timespan times of when they appear in the videos

//				var matchData = await DB.GetData<MatchData>( upload.DataName );
//				matchData.VideoType = VideoUrlType.MergedVideoLink;
//				matchData.VideoStartTime = TimeSpan.Zero;
//				matchData.VideoEndTime = matchData.GetDuration();
//				await DB.SaveData( matchData );

//				TimeSpan currentTime = TimeSpan.Zero;

//				foreach( var roundName in matchData.Rounds )
//				{
//					var roundVideoFile = DB.SharedSettings.GetRoundVideoPath( roundName );

//					if( File.Exists( roundVideoFile ) )
//					{
//						File.Delete( roundVideoFile );
//					}

//					var roundData = await DB.GetData<RoundData>( roundName );

//					roundData.VideoType = VideoUrlType.MergedVideoLink;
//					roundData.VideoStartTime = currentTime;
//					roundData.VideoEndTime = currentTime + roundData.GetDuration();

//					await DB.SaveData( roundData );

//					currentTime += roundData.GetDuration();
//				}

//				Console.WriteLine( "Done applying changes." );
//			}
//			else
//			{
//				File.Delete( videoFileTempName );
//			}

//			return success;
//		}
//	}


//}
