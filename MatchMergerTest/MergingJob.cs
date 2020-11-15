using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

namespace MatchMergerTest
{
	public class MergingJob
	{
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

			FFmpeg.ExecutablesPath = tempFFmpegFolder;
			await FFmpeg.GetLatestVersion( FFmpegVersion.Full );

			//try to find the first match that has all the video files still not uploaded

			var foundMatchName = string.Empty;

			foreach( var matchName in await GameDatabase.GetAll<MatchData>() )
			{
				MatchData matchData = await GameDatabase.GetData<MatchData>( matchName );
				//check if all its rounds have been uploaded

				bool suitable = true;

				foreach( var roundName in matchData.Rounds )
				{
					RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );

					if( !string.IsNullOrEmpty( roundData.YoutubeUrl ) || !File.Exists( GameDatabase.SharedSettings.GetRoundVideoPath( roundName ) ) )
					{
						suitable = false;
						break;
					}
				}

				if( suitable )
				{
					foundMatchName = matchName;
					break;
				}
			}

			if( !string.IsNullOrEmpty( foundMatchName ) )
			{
				//gather all the filenames for the rounds
				MatchData matchData = await GameDatabase.GetData<MatchData>( foundMatchName );

				Dictionary<RoundData , FileInfo> roundFiles = new Dictionary<RoundData , FileInfo>();

				foreach( var roundName in matchData.Rounds )
				{
					RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );

					var videoPath = GameDatabase.SharedSettings.GetRoundVideoPath( roundName );

					//check if the converted one exists first
					/*
					var convertedVideoPath = Path.ChangeExtension( videoPath , "converted.mp4" );

					if( File.Exists( convertedVideoPath ) )
					{
						videoPath = convertedVideoPath;
					}
					*/

					var videoInfo = new FileInfo( videoPath );

					if( videoInfo.Exists )
					{
						roundFiles.Add( roundData , videoInfo );
					}
				}


				if( roundFiles.Count != matchData.Rounds.Count )
				{
					throw new Exception( "FUCKING GAY" );
				}

				var orderedRoundFiles = roundFiles.OrderBy( x => x.Key.TimeStarted );


				/*
				var conversion = await Conversion.Concatenate(
					$@"C:\Users\Jvsth.000.000\Desktop\Test\{foundMatchName}.mp4" ,
					orderedRoundFiles.Select( x => x.Value.FullName ).ToArray()
				);
				*/
				//foreach( var kv in orderedRoundFiles )
				{
					var kv = orderedRoundFiles.First();
					var fileName = kv.Value.FullName;

					if( !File.Exists( $@"C:\Users\Jvsth.000.000\Desktop\Test\{kv.Key.Name}.mp4" ) )
					{
						File.Copy( fileName , $@"C:\Users\Jvsth.000.000\Desktop\Test\{kv.Key.Name}.mp4" , false );
					}

					var info = await MediaInfo.Get( fileName );

					//let's just try to lower the crf

					var conv = Conversion.New();

					foreach( var stream in info.Streams )
					{
						conv.AddStream( stream );
					}

					/*
					foreach( var strm in info.VideoStreams )
					{
						conv.AddStream( strm.CopyStream() );
					}

					foreach( var strm in info.AudioStreams )
					{
						conv.AddStream( strm.CopyStream() );
					}
					*/


					conv.SetOutputFormat( MediaFormat.Matroska );
					conv.SetOutput( $@"C:\Users\Jvsth.000.000\Desktop\Test\{kv.Key.Name}.mkv" );
					conv.SetOverwriteOutput( true );
					conv.SetVideoBitrate( "2M" );
					conv.UseHardwareAcceleration( HardwareAccelerator.Auto , VideoCodec.H264_cuvid , VideoCodec.H264_nvenc );

					await conv.Start();

					//WEBM conversion with vp9
					/*
					//two passes
					for( int pass = 1; pass < 3; pass++ )
					{
						var info = await MediaInfo.Get( fileName );
						var conv = Conversion.New();

						conv.UseMultiThread( 8 );

						foreach( var strm in info.VideoStreams )
						{
							conv.AddStream( strm.SetCodec( new VideoCodec( "libvpx-vp9" ) ) );
						}

						//don't add audio to the first pass
						if( pass == 1 )
						{
							conv.AddParameter( "-an" );
						}
						else
						{
							foreach( var strm in info.AudioStreams )
							{
								conv.AddStream( strm.SetCodec( AudioCodec.Libvorbis ) );
							}
						}

						conv.AddParameter( $"-passlogfile \"{kv.Key.Name}\"" );
						conv.AddParameter( "-b:v 1M" );
						conv.AddParameter( $"-pass {pass}" );
						conv.AddParameter( "-row-mt 1" );
						conv.SetOverwriteOutput( true );

						if( pass == 1 )
						{
							conv.SetOutputFormat( new MediaFormat( "webm" ) );
							conv.SetOutput( "NUL" );
						}
						else
						{
							conv.SetOutput( $@"C:\Users\Jvsth.000.000\Desktop\Test\{kv.Key.Name}.webm" );
						}

						Console.WriteLine( conv.Build() );

						//System.Diagnostics.Process.GetProcessById( 0 )

						if( pass == 2 )
						{

						}

						await conv.Start();

					}
					*/
					//break;
				}
				/*
				var conversion = Conversion.New();
				conversion.AddParameter( "-f concat" );
				foreach( var kv in orderedRoundFiles )
				{
					conversion.AddParameter( $"-i \"{kv.Value.FullName}\" " );
				}
				conversion.SetOutput( $@"C:\Users\Jvsth.000.000\Desktop\Test\{foundMatchName}.mp4" );
				*/
				//Console.WriteLine( conversion.Build() );
				//await conversion.Start();
			}

		}

	}
}