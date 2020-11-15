using DSharpPlus;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

/*
Goes through all the folders, puts all rounds and matches into data.json
Also returns match/round data from the timestamped name and whatnot
*/

namespace MatchUploader
{
	public sealed class MatchUploaderHandler
	{
		private BotSettings BotSettings { get; } = new BotSettings();
		private DiscordClient DiscordClient { get; }
		private IGameDatabase GameDatabase { get; }
		private string SettingsFolder { get; }
		private UploaderSettings UploaderSettings { get; } = new UploaderSettings();

		//private AuthenticationResult microsoftGraphCredentials;
		private IConfigurationRoot Configuration { get; }
		private Dictionary<VideoMirrorType , Uploader> VideoUploaders { get; } = new Dictionary<VideoMirrorType , Uploader>();
		private List<Uploader> Uploaders { get; } = new List<Uploader>();

		private HttpClient NormalHttpClient { get; } = new HttpClient();

		private JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented
		};

		public MatchUploaderHandler( string [] args )
		{

			SettingsFolder = Path.Combine( Directory.GetCurrentDirectory() , "Settings" );
			Configuration = new ConfigurationBuilder()
				.SetBasePath( SettingsFolder )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "uploader.json" )
				.AddJsonFile( "bot.json" )
				.AddCommandLine( args )
			.Build();

			Configuration.Bind( UploaderSettings );
			Configuration.Bind( BotSettings );

			//GameDatabase = new FileSystemGameDatabase();

			GameDatabase = new OctoKitGameDatabase( NormalHttpClient , UploaderSettings.GitUsername , UploaderSettings.GitPassword )
			{
				InitialLoad = true ,
			};


			Configuration.Bind( GameDatabase.SharedSettings );

			if( !string.IsNullOrEmpty( BotSettings.DiscordToken ) )
			{
				DiscordClient = new DiscordClient( new DiscordConfiguration()
				{
					AutoReconnect = true ,
					TokenType = TokenType.Bot ,
					Token = BotSettings.DiscordToken ,
				} );
			}

			CreateUploaders();
		}

		private void CreateUploaders()
		{
			//always create the calendar one
			{
				Uploader calendar = new CalendarUploader( new UploaderInfo() , GameDatabase , UploaderSettings );
				Uploaders.Add( calendar );
			}

			//youtube uploader
			switch( UploaderSettings.VideoMirrorUpload )
			{
				case VideoMirrorType.Youtube:
					{
						if( !UploaderSettings.UploadersInfo.TryGetValue( VideoMirrorType.Youtube , out UploaderInfo uploaderInfo ) )
						{
							uploaderInfo = new UploaderInfo();

							UploaderSettings.UploadersInfo.TryAdd( VideoMirrorType.Youtube , uploaderInfo );
						}

						Uploader youtube = new YoutubeUploader( uploaderInfo , GameDatabase , UploaderSettings );

						Uploaders.Add( youtube );

						VideoUploaders.TryAdd( VideoMirrorType.Youtube , youtube );
					}
					break;

				case VideoMirrorType.Discord:
					{

					}
					break;

				default:
					break;
			}




		}

		public async Task Initialize()
		{
			foreach( var uploader in Uploaders )
			{
				uploader.SaveSettingsCallback += SaveSettings;
				uploader.UpdateStatusCallback += SetDiscordPresence;

				if( !uploader.Info.HasBeenSetup )
				{
					uploader.CreateDefaultInfo();
					uploader.Info.HasBeenSetup = true;
				}

				await uploader.Initialize();
			}


			if( DiscordClient != null )
			{
				await DiscordClient.ConnectAsync();
				await DiscordClient.InitializeAsync();
			}

			/*
			Microsoft.Graph.GraphServiceClient graphService = new Microsoft.Graph.GraphServiceClient(
					"https://graph.microsoft.com/v1.0/" ,
					new Microsoft.Graph.DelegateAuthenticationProvider(
						async ( requestMessage ) => requestMessage.Headers.Authorization = new AuthenticationHeaderValue( "bearer" , userCredentials.AccessToken )
					)
				);
			*/
		}

		public async Task LoadDatabase()
		{
			Console.WriteLine( $"Loading the {GameDatabase.GetType()}" );
			await GameDatabase.Load();
			Console.WriteLine( $"Finished loading the {GameDatabase.GetType()}" );
		}

		public async Task RunAsync()
		{
			await LoadDatabase();
			await Initialize();

			SaveSettings();

			await Upload();

			UploaderSettings.LastRan = DateTime.Now;

			SaveSettings();
		}

		private async Task Upload()
		{
			foreach( var uploader in Uploaders )
			{
				await uploader.UploadAll();
			}
		}

		//in this context, settings are only the uploaderSettings
		public void SaveSettings()
		{
			using( var writer = File.CreateText( Path.Combine( SettingsFolder , "uploader.json" ) ) )
			{
				Serializer.Serialize( writer , UploaderSettings );
			}
		}

		public async Task UploadToDiscordAsync()
		{
			var uploadChannel = await DiscordClient.GetChannelAsync( UploaderSettings.DiscordUploadChannel );

			if( uploadChannel == null )
			{
				Console.WriteLine( "Discord Upload channel is null" );
				return;
			}

			var ytClient = new YoutubeExplode.YoutubeClient( NormalHttpClient );

			//go through each round, see if they already have a discord mirror, otherwise reupload

			foreach( string roundName in await GameDatabase.GetAll<RoundData>() )
			{
				RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );
				if( !string.IsNullOrWhiteSpace( roundData.YoutubeUrl ) && roundData.VideoType == VideoType.VideoLink )
				{
					VideoMirrorData discordMirror = roundData.VideoMirrors.FirstOrDefault( mirror => mirror.MirrorType == VideoMirrorType.Discord );

					//see if it already has a discord mirror
					if( discordMirror != null ) //TODO: check if url is still valid?
					{
						continue;
					}

					Console.WriteLine( $"Uploading {roundName}" );

					//if it was successfull, save the data
					string discordMirrorUrl;

					var mediaStreamInfo = await ytClient.GetVideoMediaStreamInfosAsync( roundData.YoutubeUrl );
					//get the quality that actually fits into 8 mb
					var chosenQuality = mediaStreamInfo.Muxed.FirstOrDefault( quality => quality.Size <= UploaderSettings.DiscordMaxUploadSize );

					if( chosenQuality == null )
					{
						Console.WriteLine( $"Could not find a quality that fits into {UploaderSettings.DiscordMaxUploadSize} for {roundName}" );
						continue;
					}

					using( MemoryStream videoStream = new MemoryStream() )
					{
						await ytClient.DownloadMediaStreamAsync( chosenQuality , videoStream );
						videoStream.Position = 0;
						var message = await uploadChannel.SendFileAsync( $"{roundName}.mp4" , videoStream );
						discordMirrorUrl = message.Attachments.FirstOrDefault()?.Url;
					}

					if( !string.IsNullOrEmpty( discordMirrorUrl ) )
					{
						discordMirror = new VideoMirrorData()
						{
							MirrorType = VideoMirrorType.Discord ,
							URL = discordMirrorUrl ,
						};

						roundData.VideoMirrors.Add( discordMirror );
						await GameDatabase.SaveData( roundData );
						Console.WriteLine( $"Uploaded {roundName}" );
					}
				}
			}
		}


		private async Task SetDiscordPresence( string str )
		{
			if( DiscordClient == null || DiscordClient.CurrentUser == null )
			{
				return;
			}

			if( DiscordClient.CurrentUser.Presence.Activity != null && DiscordClient.CurrentUser.Presence.Activity.Name == str )
			{
				return;
			}

			await DiscordClient.UpdateStatusAsync( new DSharpPlus.Entities.DiscordActivity( str ) );
		}


		private async Task ProcessVideo( string roundName )
		{
			RoundData roundData = await GameDatabase.GetData<RoundData>( roundName );

			string videoPath = GameDatabase.SharedSettings.GetRoundVideoPath( roundName );

			string outputPath = Path.ChangeExtension( videoPath , "converted.mp4" );

			if( roundData.RecordingType == RecordingType.Video && File.Exists( videoPath ) && !File.Exists( outputPath ) )
			{
				Console.WriteLine( $"Converting {roundName}" );

				IMediaInfo mediaInfo = await MediaInfo.Get( videoPath );
				IConversion newConversion = Conversion.New();

				bool abortConversion = mediaInfo.VideoStreams.Any( vid => vid.Bitrate < 6000000 );

				//only continue if these are videos with 6 megabits of bitrate
				if( abortConversion )
				{
					return;
				}

				foreach( var videostream in mediaInfo.VideoStreams )
				{
					newConversion.AddStream( videostream.SetCodec( VideoCodec.H264_nvenc ) );
				}

				foreach( var audiostream in mediaInfo.AudioStreams )
				{
					newConversion.AddStream( audiostream );
				}

				newConversion.SetOverwriteOutput( true );

				newConversion.SetOutput( outputPath );

				newConversion.AddParameter( "-crf 23 -maxrate 2000k -bufsize 4000k" );

				newConversion.SetPreset( ConversionPreset.Slow );
				Console.WriteLine( newConversion.Build() );
				await newConversion.Start();

				Console.WriteLine( outputPath );
			}
		}
		private async Task ProcessVideoFiles()
		{
			string tempFFmpegFolder = Path.Combine( Path.GetTempPath() , "ffmpeg" );

			if( !Directory.Exists( tempFFmpegFolder ) )
			{
				Directory.CreateDirectory( tempFFmpegFolder );
				Console.WriteLine( $"Created directory {tempFFmpegFolder}" );
			}

			FFmpeg.ExecutablesPath = tempFFmpegFolder;
			await FFmpeg.GetLatestVersion();


			List<Task> processingTasks = new List<Task>();

			foreach( string roundName in await GameDatabase.GetAll<RoundData>() )
			{
				processingTasks.Add( ProcessVideo( roundName ) );
			}

			await Task.WhenAll( processingTasks );
		}
	}
}