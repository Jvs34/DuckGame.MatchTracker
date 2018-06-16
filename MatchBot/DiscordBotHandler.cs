using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MatchTracker;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using Microsoft.Bot;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Builder.Ai.LUIS;

namespace MatchBot
{
	public class DiscordBotHandler : BotAdapter
	{
		private DiscordSocketClient discordClient;
		private SharedSettings sharedSettings;
		private BotSettings botSettings;
		private IBot bot;

		public DiscordBotHandler()
		{

			discordClient = new DiscordSocketClient( new DiscordSocketConfig()
			{
				AlwaysDownloadUsers = true,
			});

			discordClient.Connected += OnDiscordConnected;
			discordClient.Disconnected += OnDiscordDisconnected;
			discordClient.MessageReceived += OnDiscordMessage;

			bot = new MatchBot();

			sharedSettings = new SharedSettings();
			botSettings = new BotSettings();

			String settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );
			String botSettingsPath = Path.Combine( settingsFolder , "bot.json" );

			Console.WriteLine( sharedSettingsPath );

			sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
			botSettings = JsonConvert.DeserializeObject<BotSettings>( File.ReadAllText( botSettingsPath ) );

			Use( new CatchExceptionMiddleware<Exception>( async ( context , exception ) =>
			{
				await context.TraceActivity( "MatchBot Exception" , exception );
				await context.SendActivity( "Sorry, it looks like something went wrong!" );
			} ) );

			MemoryStorage dataStore = new MemoryStorage();
			Use( new ConversationState<DuckGameDatabase>( dataStore ) );

			Use( new LuisRecognizerMiddleware(
				new LuisModel( "7ca2989c-899b-40ac-a8a6-a26c887080e6" , "2b40fa31e06a440cb98b783bb2d71a73" ,
					new Uri( "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" )
				)
			) );
		}

		public async Task Initialize()
		{
			await discordClient.LoginAsync( TokenType.Bot , botSettings.discordToken );
			await discordClient.StartAsync();
			await discordClient.SetStatusAsync( UserStatus.Online );

			/*
			try
			{
				var v = discordClient.CurrentUser;

				Activity chatActivity = new Activity()
				{
					Type = ActivityTypes.Message ,
					//From = ConvertDiscordUserToChannelAccount( discordClient.CurrentUser ) ,
					//Recipient = ConvertDiscordUserToChannelAccount( discordClient.CurrentUser ) ,
					Text = "test",
				};

				ConversationParameters conv = new ConversationParameters()
				{
					Activity = chatActivity ,
					Members = new ChannelAccount [] { } ,
					//Bot = ConvertDiscordUserToChannelAccount( discordClient.CurrentUser ) ,
				};

				await microsoftBotClient.Conversations.CreateConversationAsync( conv );
			}
			catch( Exception ex )
			{
				Console.WriteLine( ex );
			}
			*/
		}




		private async Task OnDiscordMessage( SocketMessage msg )
		{
			Console.WriteLine( msg.Content );
		}

		private async Task OnDiscordDisconnected( Exception arg )
		{

		}

		private async Task OnDiscordConnected()
		{

		}


		public ChannelAccount ConvertDiscordUserToChannelAccount( SocketUser user )
		{
			return new ChannelAccount()
			{
				Id = user.Id.ToString() ,
				Name = user.Username ,
				Role = user.IsBot ? "bot" : "user"
			};
		}

		public override Task<ResourceResponse []> SendActivities( ITurnContext context , Activity [] activities )
		{
			throw new NotImplementedException();
		}

		//these ones aren't even used on ConsoleAdapter
		public override Task<ResourceResponse> UpdateActivity( ITurnContext context , Activity activity )
		{
			throw new NotImplementedException();
		}

		public override Task DeleteActivity( ITurnContext context , ConversationReference reference )
		{
			throw new NotImplementedException();
		}
	}
}
