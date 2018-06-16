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
using System.Collections.Generic;

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

		private async Task OnDiscordMessage( SocketMessage msg )
		{
			//there's no equality check on this class unfortunately
			if( msg.Author.Id == discordClient.CurrentUser.Id )
				return;

			if( !msg.MentionedUsers.Any( x => x.Id == discordClient.CurrentUser.Id ) )
				return;

			await HandleIncomingMessage( msg );
		}

		//message incoming from discord, ignoring the ones from the bot
		private async Task HandleIncomingMessage( SocketMessage msg )
		{
			Activity act = GetActivityFromMessage( msg );
			using( TurnContext context = new TurnContext( this , act ) )
			using( var typingstate = msg.Channel.EnterTypingState() )
			{
				await RunPipeline( context , BotCallback );
			}
		}

		//message outgoing from the bot
		private async Task HandleOutgoingMessage( ITurnContext context , Activity act )
		{
			ulong channelId = Convert.ToUInt64( context.Activity.ChannelId );
			ISocketMessageChannel channel = discordClient.GetChannel( channelId ) as ISocketMessageChannel;
			
			if( channel != null )
			{
				await channel.SendMessageAsync( act.Text );
			}
		}

		private async Task BotCallback( ITurnContext context )
		{
			await bot.OnTurn( context );
		}

		private Activity GetActivityFromMessage( SocketMessage msg )
		{
			String text = msg.Content;
			//TODO: clean the message of all mentions and turn them into plain text
			String mention = discordClient.CurrentUser.Mention.Replace( "!" , String.Empty );//so there's an exclamation mark here somehow, why tho
			text = text.Replace( mention , String.Empty );

			return new Activity()
			{
				Text = text ,
				ChannelId = msg.Channel.Id.ToString() ,
				From = DiscordUserToBotAccount( msg.Author ) ,
				Recipient = DiscordUserToBotAccount( discordClient.CurrentUser ) ,
				Conversation = new ConversationAccount( true , null , msg.Channel.Id.ToString() , msg.Channel.Name ),
				Timestamp = msg.Timestamp,
				Id = msg.Id.ToString(),
				Type = ActivityTypes.Message,
			};
		}

		public ChannelAccount DiscordUserToBotAccount( SocketUser user )
		{
			return new ChannelAccount()
			{
				Id = user.Id.ToString() ,
				Name = user.Username ,
				Role = user.IsBot ? RoleTypes.Bot : RoleTypes.User
			};
		}

		public override async Task<ResourceResponse []> SendActivities( ITurnContext context , Activity [] activities )
		{
			List<ResourceResponse> responses = new List<ResourceResponse>();
			foreach( Activity activity in activities )
			{
				responses.Add( new ResourceResponse( activity.Id ) );
				if( activity.Type == ActivityTypes.Message && activity.From.Role == RoleTypes.Bot )
				{
					await HandleOutgoingMessage( context , activity );
				}
			}
			return responses.ToArray();
		}

		//these ones aren't even used on ConsoleAdapter, although it would probably be nice to do that
		public override async Task<ResourceResponse> UpdateActivity( ITurnContext context , Activity activity ) => throw new NotImplementedException();
		public override async Task DeleteActivity( ITurnContext context , ConversationReference reference ) => throw new NotImplementedException();
	}
}
