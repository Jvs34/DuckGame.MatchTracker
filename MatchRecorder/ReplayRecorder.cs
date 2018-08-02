using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using MatchTracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder
{
	/// <summary>
	/// Records much lighter replays along with discord voice data
	/// </summary>
	internal sealed class ReplayRecorder : IRecorder
	{
		public bool IsRecording { get; private set; }

		public RecordingType ResultingRecordingType { get; set; }

		public String FFmpegPath
		{
			get
			{
				return Path.Combine( mainHandler.ModPath , "ThirdParty" , "ffmpeg.exe" );
			}
		}

		private readonly MatchRecorderHandler mainHandler;

		private readonly DiscordClient discordClient;
		private readonly VoiceNextExtension voiceClient;

		private VoiceNextConnection voiceConnection;

		private Task connectToVoiceChannelTask;

		private readonly DuckRecordingListener eventListener;

		private Process ffmpegProcess;

		/// <summary>
		/// Used to lookup the named pipes per discord user, the key in the dictionary is the discord user id
		/// if one is present then we'll write to that one when that user is talking
		/// </summary>
		private Dictionary<ulong , NamedPipeServerStream> ffmpegChannels;

		public ReplayRecorder( MatchRecorderHandler parent )
		{
			ResultingRecordingType = RecordingType.ReplayAndVoiceChat;
			mainHandler = parent;
			//initialize the discord bot

			eventListener = new DuckRecordingListener();
			ffmpegChannels = new Dictionary<ulong , NamedPipeServerStream>();

			if( String.IsNullOrEmpty( mainHandler.BotSettings.discordToken ) )
			{
				throw new ArgumentNullException( "discordToken is null!" );
			}

			discordClient = new DiscordClient( new DiscordConfiguration()
			{
				AutoReconnect = true ,
				TokenType = TokenType.Bot ,
				Token = mainHandler.BotSettings.discordToken ,
			} );

			discordClient.Ready += async ( eventArgs ) =>
			{
				await discordClient.InitializeAsync();
				DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopRight , "Connected to discord!!!" );
			};

			voiceClient = discordClient.UseVoiceNext( new VoiceNextConfiguration()
			{
				EnableIncoming = false ,
				VoiceApplication = VoiceApplication.Voice ,
			} );

			discordClient.ConnectAsync();
		}

		private async Task ConnectToVoiceChat()
		{
			//TODO: use a discord rpc library or something to get the id of the user to stalk
			var discordUserIDToStalk = mainHandler.BotSettings.discordUserToStalk;

			foreach( var guildKV in discordClient.Guilds )
			{
				var stalked = await guildKV.Value.GetMemberAsync( discordUserIDToStalk );
				if( stalked != null && stalked.VoiceState != null )
				{
					//see if we can get the connection again first
					try
					{
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
					catch( Exception e )
					{
						Debugger.Log( 1 , "Duck" , e.Message );
					}
				}
			}
		}

		public void StartRecording()
		{
			IsRecording = true;
			DateTime recordingTime = DateTime.Now;
			String roundPath = Path.Combine( mainHandler.RoundsFolder , mainHandler.GameDatabase.sharedSettings.DateTimeToString( recordingTime ) );
			Directory.CreateDirectory( roundPath );
			mainHandler.StartCollectingRoundData( recordingTime );

			if( voiceConnection != null )
			{
				voiceConnection.VoiceReceived += OnVoiceReceived;
			}

			StartFFmpeg();
		}

		public void StopRecording()
		{
			IsRecording = false;
			mainHandler.StopCollectingRoundData( DateTime.Now );

			if( voiceConnection != null )
			{
				voiceConnection.VoiceReceived -= OnVoiceReceived;
			}

			StopFFmpeg();
		}

		public void Update()
		{
			if( voiceConnection == null && ( connectToVoiceChannelTask == null || connectToVoiceChannelTask.IsCompleted ) )
			{
				connectToVoiceChannelTask = ConnectToVoiceChat();
			}

			if( DuckGame.Recorder.currentRecording != eventListener )
			{
				DuckGame.Recorder.currentRecording = eventListener;
			}

			eventListener.UpdateEvents();
		}

		private bool StartFFmpeg()
		{
			if( voiceConnection == null )
				return false;

			String inputArg = "";


			var channelMembers = voiceConnection.Channel.Users;

			//make a pipe for each user by using their discord id
			foreach( var member in channelMembers )
			{
				if( member == discordClient.CurrentUser )
				{
					continue;
				}

				//TODO: check if the user is actually in the game so we can skip people that aren't
				var namedPipe = new NamedPipeServerStream( $"{member.Id}" , PipeDirection.Out , 1 , PipeTransmissionMode.Byte , PipeOptions.Asynchronous , 10000 , 10000 );

				ffmpegChannels.Add( member.Id , namedPipe );
				//add this named pipe to the input
				String pipe = $@"\\.\pipe\{member.Id}";

				inputArg += $" -i {pipe}";
			}


			String outputArg = $@"-codec:a libopus -q:a 0 -filter_complex amix=inputs={ffmpegChannels.Count} {Path.Combine( mainHandler.ModPath , "ThirdParty" , "test.ogg" )}";

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = FFmpegPath ,
					UseShellExecute = false ,
					CreateNoWindow = false ,
					RedirectStandardInput = true ,
					Arguments = $"{inputArg} {outputArg}" ,
				}
			};

			if( process.Start() )
			{
				ffmpegProcess = process;
				return true;
			}

			return false;
		}

		private void StopFFmpeg()
		{
			foreach( var channelskv in ffmpegChannels )
			{
				var pipe = channelskv.Value;

				if( pipe.IsConnected )
				{
					pipe.Flush();
				}

				pipe.Dispose();
			}

			ffmpegChannels.Clear();

			if( ffmpegProcess != null && !ffmpegProcess.HasExited )
			{
				ffmpegProcess.StandardInput.BaseStream.Close();
				ffmpegProcess.WaitForExit();
			}
		}

		private bool HasNamedPipe( DiscordUser user )
		{
			return ffmpegChannels.ContainsKey( user.Id );
		}

		private async Task OnVoiceReceived( VoiceReceiveEventArgs args )
		{
			if( ffmpegChannels.TryGetValue( args.User.Id , out NamedPipeServerStream stream ) )
			{
				await stream.WaitForConnectionAsync();
				await stream.WriteAsync( args.Voice.ToArray() , 0 , args.VoiceLength );
			}
		}
	}
}
