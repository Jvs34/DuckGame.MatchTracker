﻿using System;
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
using System.Linq;

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
			discordClient = new DiscordSocketClient();

			discordClient.Connected += OnDiscordConnected;
			discordClient.Disconnected += OnDiscordDisconnected;
			discordClient.MessageReceived += OnDiscordMessage;
			discordClient.Ready += OnDiscordReady;

			bot = new MatchBot();

			sharedSettings = new SharedSettings();
			botSettings = new BotSettings();

			String settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );
			String botSettingsPath = Path.Combine( settingsFolder , "bot.json" );

			Console.WriteLine( sharedSettingsPath );

			sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
			botSettings = JsonConvert.DeserializeObject<BotSettings>( File.ReadAllText( botSettingsPath ) );

			//add middleware to our botadapter stuff
			//TODO: we might need a json datastore for this later? who knows, maybe make it use the botSettings shit
			MemoryStorage dataStore = new MemoryStorage();

			Use( new CatchExceptionMiddleware<Exception>( async ( context , exception ) =>
			{
				await context.TraceActivity( "MatchBot Exception" , exception );
				await context.SendActivity( "Sorry, it looks like something went wrong!" );
			} ) );
			Use( new ConversationState<DuckGameDatabase>( dataStore ) );
			Use( new LuisRecognizerMiddleware( new LuisModel( botSettings.luisModelId , botSettings.luisSubcriptionKey , botSettings.luisUri ) ) );

		}


		public async Task Initialize()
		{
			await discordClient.LoginAsync( TokenType.Bot , botSettings.discordToken );
			await discordClient.StartAsync();
			await discordClient.SetStatusAsync( UserStatus.Online );
		}

		private async Task OnDiscordConnected()
		{
			Console.WriteLine( "Connected to Discord" );
		}

		private async Task OnDiscordDisconnected( Exception arg )
		{
			Console.WriteLine( "Disconnected from Discord {0}" , arg.ToString() );
		}

		private async Task OnDiscordReady()
		{
			Console.WriteLine( discordClient.CurrentUser );
		}

		private async Task OnDiscordMessage( SocketMessage msg )
		{
			//I dunno if this'll happen but we don't care about our own messages
			if( msg.Author == discordClient.CurrentUser )
				return;

			/*
			if( !msg.MentionedUsers.Contains( discordClient.CurrentUser ) )
				return;
			*/

			Console.WriteLine( msg.Content );
			await HandleIncomingMessage( msg );
			
		}

		private async Task HandleIncomingMessage( SocketMessage msg )
		{
			Activity act = GetActivityFromMessage( msg );
			using( TurnContext context = new TurnContext( this , act ) )
			{
				await RunPipeline( context , null , null );
			}
		}

		private Activity GetActivityFromMessage( SocketMessage msg )
		{
			return new Activity()
			{
				Text = msg.Content ,
				ChannelId = msg.Channel.Name , //this is a little backwards but we only care about the nice name, not the actual id here
				From = DiscordUserToBotAccount( msg.Author ) ,
				Recipient = DiscordUserToBotAccount( discordClient.CurrentUser ) ,
				Conversation = new ConversationAccount( true , null , msg.Channel.Id.ToString() , msg.Channel.Name ),
				Timestamp = msg.Timestamp,
				Id = msg.Id.ToString(),
				Type = "message"
			};
		}

		public ChannelAccount DiscordUserToBotAccount( SocketUser user )
		{
			return new ChannelAccount()
			{
				Id = user.Id.ToString() ,
				Name = user.Username ,
				Role = user.IsBot ? "bot" : "user"
			};
		}

		public override async Task<ResourceResponse []> SendActivities( ITurnContext context , Activity [] activities ) => throw new NotImplementedException();

		//these ones aren't even used on ConsoleAdapter, although it would probably be nice to do that
		public override async Task<ResourceResponse> UpdateActivity( ITurnContext context , Activity activity ) => throw new NotImplementedException();
		public override async Task DeleteActivity( ITurnContext context , ConversationReference reference ) => throw new NotImplementedException();
	}
}
