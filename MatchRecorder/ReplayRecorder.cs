/*
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
*/
using DuckGame;
using MatchTracker;
using MatchTracker.Replay;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder
{
	/// <summary>
	/// Records much lighter replays along with discord voice data
	/// </summary>
	internal sealed class ReplayRecorder : IRecorder
	{
		/*
		private readonly DiscordClient discordClient;
		private readonly VoiceNextExtension voiceClient;
		*/
		private readonly DuckRecordingListener eventListener;
		private Task connectToVoiceChannelTask;
		private readonly MatchRecorderHandler mainHandler;

		/// <summary>
		/// Used to lookup the named pipes per discord user, the key in the dictionary is the discord user id
		/// if one is present then we'll write to that one when that user is talking
		/// </summary>
		private Dictionary<ulong , NamedPipeServerStream> ffmpegChannels;

		private Process ffmpegProcess;
		//private VoiceNextConnection voiceConnection;
		private bool CatchingDrawCalls { get; set; }

		public string FFmpegPath
		{
			get
			{
				return Path.Combine( mainHandler.ModPath , "ThirdParty" , "ffmpeg.exe" );
			}
		}

		public bool IsRecording { get; private set; }

		public RecordingType ResultingRecordingType { get; set; }
		public ReplayRecording CurrentRecording { get; private set; }

		public ReplayRecorder( MatchRecorderHandler parent )
		{
			ResultingRecordingType = RecordingType.ReplayAndVoiceChat;
			mainHandler = parent;
			//initialize the discord bot

			eventListener = new DuckRecordingListener();
			ffmpegChannels = new Dictionary<ulong , NamedPipeServerStream>();

			if( string.IsNullOrEmpty( mainHandler.BotSettings.DiscordToken ) )
			{
				throw new ArgumentNullException( "discordToken is null!" );
			}

			ReplayRecording.InitProtoBuf();

			/*
			discordClient = new DiscordClient( new DiscordConfiguration()
			{
				AutoReconnect = true ,
				TokenType = TokenType.Bot ,
				Token = mainHandler.BotSettings.DiscordToken ,
			} );

			discordClient.Ready += async ( eventArgs ) =>
			{
				await discordClient.InitializeAsync();
				DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopRight , "Connected to discord!!!" );
			};

			voiceClient = discordClient.UseVoiceNext( new VoiceNextConfiguration()
			{
				EnableIncoming = true ,
				VoiceApplication = VoiceApplication.Voice ,
			} );

			discordClient.ConnectAsync();
			*/
		}

		public void StartRecording()
		{
			IsRecording = true;
			DateTime recordingTime = DateTime.Now;
			string roundPath = Path.Combine( mainHandler.RoundsFolder , mainHandler.GameDatabase.SharedSettings.DateTimeToString( recordingTime ) );
			Directory.CreateDirectory( roundPath );
			RoundData roundData = mainHandler.StartCollectingRoundData( recordingTime );

			CurrentRecording = new ReplayRecording()
			{
				TimeStarted = roundData.TimeStarted ,
				Name = roundData.Name ,
			};

			/*
			if( voiceConnection != null )
			{
				voiceConnection.VoiceReceived += OnVoiceReceived;
			}
			*/

			//StartFFmpeg();
		}

		public void StopRecording()
		{
			IsRecording = false;
			RoundData roundData = mainHandler.StopCollectingRoundData( DateTime.Now );

			if( roundData != null )
			{

				CurrentRecording.TimeEnded = roundData.TimeEnded;
				//now save the current recording too

				string recordingPath = Path.Combine( mainHandler.RoundsFolder , roundData.Name );

				ReplayRecording rec = CurrentRecording;

				Task.Factory.StartNew( () => SaveReplay( roundData.Name , rec , mainHandler.GameDatabase.SharedSettings ) );

				CurrentRecording = null;
			}

			/*
			if( voiceConnection != null )
			{
				voiceConnection.VoiceReceived -= OnVoiceReceived;
			}
			*/

			//StopFFmpeg();
		}

		private void SaveReplay( string roundName , ReplayRecording recording , SharedSettings sharedSettings )
		{
			using( var fileStream = File.OpenWrite( sharedSettings.GetRoundReplayPath( roundName ) ) )
			using( ZipArchive archive = new ZipArchive( fileStream , ZipArchiveMode.Create ) )
			{
				// recording.TrimDrawCalls();
				ZipArchiveEntry replayEntry = archive.CreateEntry( sharedSettings.RoundReplayFile );
				recording.Serialize( replayEntry.Open() );
			}
		}

		public void Update()
		{   /*
			if( voiceConnection == null && ( connectToVoiceChannelTask == null || connectToVoiceChannelTask.IsCompleted ) )
			{
				//connectToVoiceChannelTask = ConnectToVoiceChat();
			}
			*/

			if( DuckGame.Recorder.currentRecording != eventListener )
			{
				DuckGame.Recorder.currentRecording = eventListener;
			}

			eventListener.UpdateEvents();
		}

		public void StartFrame()
		{
			CatchingDrawCalls = true;

			var currentRound = mainHandler.CurrentRound;

			if( currentRound == null || CurrentRecording == null )
				return;

			var camera = Level.current?.camera;
			MatchTracker.Rectangle cameraData = null;

			if( camera != null )
			{
				cameraData = new MatchTracker.Rectangle()
				{
					Width = camera.width ,
					Height = camera.height ,
					Position = new MatchTracker.Vec2()
					{
						X = camera.x ,
						Y = camera.y ,
					}
				};
			}

			CurrentRecording.StartFrame( DateTime.Now.Subtract( currentRound.TimeStarted ) , cameraData );

			//Level.current.camera.position , width, height
			//if it's a followcam we need to save the zoom I think?
		}

		public void EndFrame()
		{
			CurrentRecording?.EndFrame();
			CatchingDrawCalls = false;
		}

		public void OnTextureDraw( Tex2D texture , DuckGame.Vec2 position , DuckGame.Rectangle? sourceRectangle , DuckGame.Color color , float rotation , DuckGame.Vec2 origin , DuckGame.Vec2 scale , int effects , Depth depth = default )
		{
			//TODO: for now ignore draw calls that are done outside of startframe and endframe
			if( !CatchingDrawCalls )
			{
				//this is apparently done by some render target stuff like when ducks are outside the camera's view
				return;
			}

			string textureName = texture.textureName;

			//TODO: handle render targets better
			if( textureName == "__internal" )
			{
				return;
			}

            if ( textureName == "natureTileset" )
            {
                Console.WriteLine( "Penis" );
            }

			//texture.
			if( !CurrentRecording.Textures.Contains( textureName ) )
			{
				CurrentRecording.Textures.Add( textureName );
			}

			if( Graphics.material != null && !CurrentRecording.Materials.Contains( Graphics.material.effect.effectName ) )
			{
				CurrentRecording.Materials.Add( Graphics.material.effect.effectName );
				//TODO: save material parameters? unless I add xna references I can't really do that
				//unless I do a lot of reflection hackery
			}


			//Graphics.currentLayer
			//Graphics.currentDrawingObject;
			MatchTracker.Rectangle texCoords = null;

			int entityIndex = Graphics.currentDrawingObject?.GetHashCode() ?? -1;

			if( sourceRectangle.HasValue )
			{
				texCoords = new MatchTracker.Rectangle()
				{
					Position = new MatchTracker.Vec2()
					{
						X = sourceRectangle.Value.x ,
						Y = sourceRectangle.Value.y ,
					} ,
					Width = sourceRectangle.Value.width ,
					Height = sourceRectangle.Value.height ,
				};
			}
			else
			{
				texCoords = new MatchTracker.Rectangle()
				{
					Position = new MatchTracker.Vec2()
					{
						X = 0 ,
						Y = 0 ,
					} ,
					Width = texture.width ,
					Height = texture.height ,
				};
			}

			//TODO: check effects and flip stuff accordingly? this is already saved in ReplayFrameItem though
			switch( effects )
			{
				case 1:
					{
						//origin.x = (sourceRectangle.HasValue ? sourceRectangle.Value.width : ((float)texture.w)) - origin.x;
						//FlipHorizontally
						break;
					}
				case 2:
					{
						//FlipVertically
						break;
					}
				default:
					break;
			}


			CurrentRecording.AddDrawCall( textureName ,
				new MatchTracker.Vec2()
				{
					X = position.x ,
					Y = position.y
				} ,
				texCoords ,
				new MatchTracker.Color()
				{
					R = color.r ,
					G = color.g ,
					B = color.b ,
					A = color.a ,
				} ,
				rotation ,
				new MatchTracker.Vec2()
				{
					X = origin.x ,
					Y = origin.y ,
				} ,
				new MatchTracker.Vec2()
				{
					X = scale.x ,
					Y = scale.y ,
				} ,
				effects ,
				depth.value ,
				entityIndex
			);
		}
        
        public int OnStartStaticDraw()
        {
			if( !CatchingDrawCalls || CurrentRecording == null )
			    return 0;

            return CurrentRecording.OnStartStaticDraw();
        }

        public void OnFinishStaticDraw()
        {
			if( !CatchingDrawCalls )
			    return;

            CurrentRecording?.OnFinishStaticDraw();
        }

        public void OnStaticDraw( int id )
        {
			if( !CatchingDrawCalls )
			    return;

			CurrentRecording?.OnStaticDraw( id );
        }

		/*
		private async Task ConnectToVoiceChat()
		{
			//TODO: use a discord rpc library or something to get the id of the user to stalk
			var discordUserIDToStalk = mainHandler.BotSettings.DiscordUserToStalk;

			foreach( var guildKV in discordClient.Guilds )
			{
				var stalked = await guildKV.Value.GetMemberAsync( discordUserIDToStalk );
				if( stalked != null && stalked.VoiceState != null )
				{
					//see if we can get the connection again first

					var newVoiceConnection = voiceClient.GetConnection( stalked.Guild );

					//otherwise try connecting
					if( newVoiceConnection == null )
					{
						newVoiceConnection = await voiceClient.ConnectAsync( stalked.VoiceState.Channel );
					}

					if( newVoiceConnection != null )
					{
						voiceConnection = newVoiceConnection;
					}
				}
			}
		}

		private bool HasNamedPipe( DiscordUser user )
		{
			return ffmpegChannels.ContainsKey( user.Id );
		}
		*/
		//private async Task OnVoiceReceived( VoiceReceiveEventArgs args )
		//{
		//	try
		//	{
		//		if( IsRecording && ffmpegProcess != null && !ffmpegProcess.HasExited )
		//		{
		//			await ffmpegProcess.StandardInput.BaseStream.WriteAsync( args.Voice.ToArray() , 0 , args.VoiceLength );
		//		}
		//		/*
		//		if( ffmpegChannels.TryGetValue( args.User.Id , out NamedPipeServerStream stream ) )
		//		{
		//			if( !stream.IsConnected )
		//			{
		//				await stream.WaitForConnectionAsync();
		//			}

		//			await stream.WriteAsync( args.Voice.ToArray() , 0 , args.VoiceLength );
		//		}
		//		*/
		//	}
		//	catch( Exception e )
		//	{
		//		Debug.WriteLine( e );
		//	}
		//}

		//private bool StartFFmpeg()
		//{
		//	if( voiceConnection == null )
		//		return false;

		//	string inputArg = @"-ac 1 -f s16le -ar 48000";

		//	var channelMembers = voiceConnection.Channel.Users;

		//	//make a pipe for each user by using their discord id
		//	/*
		//	foreach( var member in channelMembers )
		//	{
		//		if( member == discordClient.CurrentUser )
		//		{
		//			continue;
		//		}

		//		//TODO: check if the user is actually in the game so we can skip people that aren't
		//		var namedPipe = new NamedPipeServerStream( $"{member.Id}" , PipeDirection.Out , 1 , PipeTransmissionMode.Byte , PipeOptions.Asynchronous , 10000 , 10000 );

		//		ffmpegChannels.Add( member.Id , namedPipe );
		//		//add this named pipe to the input
		//		String pipe = $@"\\.\pipe\{member.Id}";

		//		inputArg += $" -i {pipe}";
		//	}
		//	*/
		//	inputArg += @"-i pipe:0";

		//	//String outputArg = $"-codec:a libopus -filter_complex amix=inputs={ffmpegChannels.Count} {Path.Combine( mainHandler.ModPath , "ThirdParty" , "test.ogg" )}";
		//	//String outputArg = $"-codec:a libopus {Path.Combine( mainHandler.ModPath , "ThirdParty" , "test.ogg" )}";
		//	string outputArg = $@"-ac 1 -ar 44100 {Path.Combine( mainHandler.ModPath , "ThirdParty" , "test.wav" )}";

		//	var process = new Process()
		//	{
		//		StartInfo = new ProcessStartInfo()
		//		{
		//			FileName = FFmpegPath ,
		//			UseShellExecute = false ,
		//			CreateNoWindow = false ,
		//			Arguments = $"{inputArg} {outputArg}" ,
		//			RedirectStandardInput = true ,
		//		}
		//	};

		//	try
		//	{
		//		if( process.Start() )
		//		{
		//			ffmpegProcess = process;
		//			return true;
		//		}
		//	}
		//	catch( Exception e )
		//	{
		//		Debug.WriteLine( e );
		//	}

		//	return false;
		//}

		//private void StopFFmpeg()
		//{
		//	/*
		//	foreach( var channelskv in ffmpegChannels )
		//	{
		//		var pipe = channelskv.Value;

		//		if( pipe.IsConnected )
		//		{
		//			pipe.Flush();
		//			pipe.WaitForPipeDrain();
		//		}
		//		pipe.Dispose();
		//	}

		//	ffmpegChannels.Clear();
		//	*/

		//	if( ffmpegProcess != null && !ffmpegProcess.HasExited )
		//	{
		//		ffmpegProcess.StandardInput.BaseStream.Flush();
		//		ffmpegProcess.StandardInput.BaseStream.Close();
		//		ffmpegProcess.WaitForExit();
		//	}
		//}
	}
}
