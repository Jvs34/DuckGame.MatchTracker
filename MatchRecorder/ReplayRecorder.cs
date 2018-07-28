using DSharpPlus;
using System;


namespace MatchRecorder
{
	/// <summary>
	/// Records much lighter replays along with discord voice data
	/// </summary>
	public sealed class ReplayRecorder : IRecorder
	{
		public bool IsRecording { get; private set; }

		private readonly MatchRecorderHandler mainHandler;

		private readonly DiscordClient discordClient;

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

			discordClient.Ready += async (eventArgs) =>
			{
				DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopRight , "Connected to discord!!!" );
			};

			discordClient.ConnectAsync();
		}

		public void StartRecording()
		{
		}

		public void StopRecording()
		{
		}

		public void Update()
		{
		}
	}
}
