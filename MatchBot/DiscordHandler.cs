using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Bot.Connector;
using Microsoft.Rest;
using MatchTracker;
using System.IO;
using Newtonsoft.Json;

namespace MatchBot
{
	public class DiscordHandler
	{
		private DiscordSocketClient discordClient;
		private ConnectorClient microsoftBotClient;
		private SharedSettings sharedSettings;
		private BotSettings botSettings;

		public DiscordHandler()
		{
			discordClient = new DiscordSocketClient();
			discordClient.Connected += OnDiscordConnected;
			discordClient.Disconnected += OnDiscordDisconnected;
			discordClient.MessageReceived += OnDiscordMessage;

			microsoftBotClient = new ConnectorClient( new Uri( "https://localhost" ) );

			sharedSettings = new SharedSettings();
			botSettings = new BotSettings();

			String dir = Directory.GetParent( Directory.GetCurrentDirectory() ).FullName;
			String settingsFolder = Path.Combine( Path.GetFullPath( dir ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );
			String botSettingsPath = Path.Combine( settingsFolder , "bot.json" );

			Console.WriteLine( sharedSettingsPath );

			sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
			botSettings = JsonConvert.DeserializeObject<BotSettings>( File.ReadAllText( botSettingsPath ) );
		}

		public async Task Initialize()
		{
			await discordClient.LoginAsync( TokenType.Bot , botSettings.discordToken );
		}



		private async Task OnDiscordMessage( SocketMessage arg )
		{
			
		}

		private async Task OnDiscordDisconnected( Exception arg )
		{
			
		}

		private async Task OnDiscordConnected()
		{
			
		}
	}
}
