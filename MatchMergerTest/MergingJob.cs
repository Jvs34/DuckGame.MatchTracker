using Humanizer;
using Humanizer.Bytes;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace MatchMergerTest
{
	public class MergingJob
	{
		class Fuckingprogress : IProgress<ProgressInfo>
		{
			public void Report( ProgressInfo value )
			{
				ByteSize byteSize = ByteSize.FromBytes( value.DownloadedBytes );
				float prgr = (float) value.DownloadedBytes / (float) value.TotalBytes * 100f;
				Console.WriteLine( $"Downloaded:  {(int) prgr}% {byteSize.ToFullWords()}" );
			}
		}

		public IConfigurationRoot Configuration { get; }
		public IGameDatabase GameDatabase { get; }

		public MergingJob()
		{
			GameDatabase = new FileSystemGameDatabase();

			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
			.Build();

			Configuration.Bind( GameDatabase.SharedSettings );
		}

		public async Task RunAsync()
		{
			await GameDatabase.Load();

			string tempFFmpegFolder = Path.Combine( Path.GetTempPath() , "ffmpeg" );

			if( !Directory.Exists( tempFFmpegFolder ) )
			{
				Directory.CreateDirectory( tempFFmpegFolder );
				Console.WriteLine( $"Created directory {tempFFmpegFolder}" );
			}

			FFmpeg.SetExecutablesPath( tempFFmpegFolder );

			await FFmpegDownloader.GetLatestVersion( FFmpegVersion.Official , FFmpeg.ExecutablesPath , new Fuckingprogress() );

			//try to find the first match that has all the video files still not uploaded
			var foundMatches = await GetEligibleMatchNames();
			//var foundMatchName = foundMatches.FirstOrDefault();
			foreach( var foundMatchName in foundMatches )
			{
				var outputPath = $@"C:\Users\Jvsth.000.000\Desktop\Test\{foundMatchName}.mp4";

				//gather all the filenames for the rounds
				MatchData matchData = await GameDatabase.GetData<MatchData>( foundMatchName );

				Dictionary<RoundData , FileInfo> roundFiles = new Dictionary<RoundData , FileInfo>();

				foreach( var roundName in matchData.Rounds )
				{
					RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );

					var videoPath = GameDatabase.SharedSettings.GetRoundVideoPath( roundName );

					var videoInfo = new FileInfo( videoPath );

					if( videoInfo.Exists )
					{
						roundFiles.Add( roundData , videoInfo );
					}
				}

				if( roundFiles.Count == matchData.Rounds.Count && !File.Exists( outputPath ) )
				{
					var orderedRoundFiles = roundFiles.OrderBy( x => x.Key.TimeStarted );
					var conversion = await FFmpeg.Conversions.FromSnippet.Concatenate(
						outputPath ,
						orderedRoundFiles.Select( x => x.Value.FullName ).ToArray()
					);
					//conversion.UseMultiThread( true );
					//conversion.UseHardwareAcceleration( HardwareAccelerator.auto , VideoCodec.h264 , VideoCodec.h264 );
					Console.WriteLine( $"Starting conversion of {matchData.Name}" );
					Stopwatch stopwatch = new Stopwatch();
					stopwatch.Start();
					await conversion.Start();
					stopwatch.Stop();

					Console.WriteLine( $"Conversion of {matchData.Name} took {stopwatch.Elapsed.Humanize()}" );
				}
			}

		}

		public async Task<List<string>> GetEligibleMatchNames()
		{
			var concMatches = new ConcurrentBag<MatchData>();
			//var concMatchNamesList = new ConcurrentBag<string>();

			await GameDatabase.IterateOverAll<MatchData>( async ( matchData ) =>
			{
				bool suitable = matchData.VideoType == VideoType.PlaylistLink;

				if( suitable )
				{
					foreach( var roundName in matchData.Rounds )
					{
						RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );

						if( !string.IsNullOrEmpty( roundData.YoutubeUrl ) || !File.Exists( GameDatabase.SharedSettings.GetRoundVideoPath( roundName ) ) )
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
	}
}