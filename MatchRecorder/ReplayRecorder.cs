using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MatchRecorder
{
	/// <summary>
	/// Records much lighter replays along with discord voice data
	/// </summary>
	internal sealed class ReplayRecorder : IRecorder
	{
		public bool IsRecording { get; private set; }

		public String FFmpegPath
		{
			get
			{
				return Path.Combine( mainHandler.ModPath , "ThirdParty" , "ffmpeg.exe" );
			}
		}

		private readonly MatchRecorderHandler mainHandler;

		private readonly DiscordClient discordClient;
		private readonly VoiceNextClient voiceClient;

		private VoiceNextConnection voiceConnection;

		private Task connectToVoiceChannelTask;

		public ReplayRecorder( MatchRecorderHandler parent )
		{
			mainHandler = parent;
			//initialize the discord bot

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
			var discordUserIDToStalk = mainHandler.BotSettings.discordUserToStalk;

			foreach( var guildKV in discordClient.Guilds )
			{
				var stalked = await guildKV.Value.GetMemberAsync( discordUserIDToStalk );
				if( stalked != null && stalked.VoiceState != null )
				{
					voiceConnection = await voiceClient.ConnectAsync( stalked.VoiceState.Channel );
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
		}

		public void StopRecording()
		{
			IsRecording = false;
			mainHandler.StopCollectingRoundData( DateTime.Now );

			if( voiceConnection != null )
			{
				voiceConnection.VoiceReceived -= OnVoiceReceived;
			}
		}

		public void Update()
		{
			if( connectToVoiceChannelTask == null || connectToVoiceChannelTask.IsCompleted )
			{
				if( voiceConnection == null )
				{
					connectToVoiceChannelTask = ConnectToVoiceChat();
				}
			}
		}

		private async Task OnVoiceReceived( VoiceReceiveEventArgs args )
		{

		}
	}
}
