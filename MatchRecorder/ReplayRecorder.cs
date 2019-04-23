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
		private readonly DiscordClient discordClient;
		private readonly DuckRecordingListener eventListener;
		private readonly MatchRecorderHandler mainHandler;
		private readonly VoiceNextExtension voiceClient;
		private Task connectToVoiceChannelTask;

		/// <summary>
		/// Used to lookup the named pipes per discord user, the key in the dictionary is the discord user id
		/// if one is present then we'll write to that one when that user is talking
		/// </summary>
		private Dictionary<ulong , NamedPipeServerStream> ffmpegChannels;

		private Process ffmpegProcess;
		private VoiceNextConnection voiceConnection;

		public string FFmpegPath
		{
			get
			{
				return Path.Combine( mainHandler.ModPath , "ThirdParty" , "ffmpeg.exe" );
			}
		}

		public bool IsRecording { get; private set; }

		public RecordingType ResultingRecordingType { get; set; }

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
		}

		public void StartRecording()
		{
			IsRecording = true;
			DateTime recordingTime = DateTime.Now;
			string roundPath = Path.Combine( mainHandler.RoundsFolder , mainHandler.GameDatabase.SharedSettings.DateTimeToString( recordingTime ) );
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
				//connectToVoiceChannelTask = ConnectToVoiceChat();
			}

			if( DuckGame.Recorder.currentRecording != eventListener )
			{
				DuckGame.Recorder.currentRecording = eventListener;
			}

			eventListener.UpdateEvents();
		}

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

		private async Task OnVoiceReceived( VoiceReceiveEventArgs args )
		{
			try
			{
				if( IsRecording && ffmpegProcess != null && !ffmpegProcess.HasExited )
				{
					await ffmpegProcess.StandardInput.BaseStream.WriteAsync( args.Voice.ToArray() , 0 , args.VoiceLength );
				}
				/*
				if( ffmpegChannels.TryGetValue( args.User.Id , out NamedPipeServerStream stream ) )
				{
					if( !stream.IsConnected )
					{
						await stream.WaitForConnectionAsync();
					}

					await stream.WriteAsync( args.Voice.ToArray() , 0 , args.VoiceLength );
				}
				*/
			}
			catch( Exception e )
			{
				Debug.WriteLine( e );
			}
		}

		private bool StartFFmpeg()
		{
			if( voiceConnection == null )
				return false;

			string inputArg = @"-ac 1 -f s16le -ar 48000";

			var channelMembers = voiceConnection.Channel.Users;

			//make a pipe for each user by using their discord id
			/*
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
			*/
			inputArg += @"-i pipe:0";

			//String outputArg = $"-codec:a libopus -filter_complex amix=inputs={ffmpegChannels.Count} {Path.Combine( mainHandler.ModPath , "ThirdParty" , "test.ogg" )}";
			//String outputArg = $"-codec:a libopus {Path.Combine( mainHandler.ModPath , "ThirdParty" , "test.ogg" )}";
			string outputArg = $@"-ac 1 -ar 44100 {Path.Combine( mainHandler.ModPath , "ThirdParty" , "test.wav" )}";

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = FFmpegPath ,
					UseShellExecute = false ,
					CreateNoWindow = false ,
					Arguments = $"{inputArg} {outputArg}" ,
					RedirectStandardInput = true ,
				}
			};

			try
			{
				if( process.Start() )
				{
					ffmpegProcess = process;
					return true;
				}
			}
			catch( Exception e )
			{
				Debug.WriteLine( e );
			}

			return false;
		}

		private void StopFFmpeg()
		{
			/*
			foreach( var channelskv in ffmpegChannels )
			{
				var pipe = channelskv.Value;

				if( pipe.IsConnected )
				{
					pipe.Flush();
					pipe.WaitForPipeDrain();
				}
				pipe.Dispose();
			}

			ffmpegChannels.Clear();
			*/

			if( ffmpegProcess != null && !ffmpegProcess.HasExited )
			{
				ffmpegProcess.StandardInput.BaseStream.Flush();
				ffmpegProcess.StandardInput.BaseStream.Close();
				ffmpegProcess.WaitForExit();
			}
		}
	}
}