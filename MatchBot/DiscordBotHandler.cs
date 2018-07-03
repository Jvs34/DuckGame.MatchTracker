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
using System.Linq;
using System.Collections.Generic;

namespace MatchBot
{
	public class DiscordBotHandler : BotAdapter
	{
		private DiscordSocketClient discordClient;
		private BotSettings botSettings;
		private MatchBot bot;

		public DiscordBotHandler()
		{
			discordClient = new DiscordSocketClient();

			discordClient.Connected += OnDiscordConnected;
			discordClient.Disconnected += OnDiscordDisconnected;
			discordClient.MessageReceived += OnDiscordMessage;

			bot = new MatchBot();

			botSettings = new BotSettings();

			String settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			String botSettingsPath = Path.Combine( settingsFolder , "bot.json" );
			botSettings = JsonConvert.DeserializeObject<BotSettings>( File.ReadAllText( botSettingsPath ) );

			//add middleware to our botadapter stuff
			//TODO: we might need a json datastore for this later? who knows, maybe make it use the botSettings shit
			//MemoryStorage dataStore = new MemoryStorage();

			Use( new CatchExceptionMiddleware<Exception>( async ( context , exception ) =>
			{
				await context.TraceActivity( "MatchBot Exception" , exception );
				await context.SendActivity( "Sorry, it looks like something went wrong!" );
			} ) );

			//Use( new ConversationState<GameDatabase>( dataStore ) ); //don't actually need this I think

			Use( new LuisRecognizerMiddleware( new LuisModel( botSettings.luisModelId , botSettings.luisSubcriptionKey , botSettings.luisUri ) ) );
		}

		public async Task Initialize()
		{
			await bot.Initialize();
			await discordClient.LoginAsync( TokenType.Bot , botSettings.discordToken );
			await discordClient.StartAsync();
			await discordClient.SetStatusAsync( UserStatus.Online );
		}

		private async Task OnDiscordConnected()
		{
			await Task.CompletedTask;
			Console.WriteLine( "Connected to Discord" );
		}

		private async Task OnDiscordDisconnected( Exception arg )
		{
			await Task.CompletedTask;
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
			using( IDisposable disposable = msg.Channel.EnterTypingState() )
			{
				await RunPipeline( context , BotCallback );
			}
		}

		//message outgoing from the bot
		private async Task HandleOutgoingMessage( ITurnContext context , Activity act )
		{
			//get the channel the original message came from
			ulong channelId = Convert.ToUInt64( context.Activity.ChannelId );

			//shouldn't this already be an ISocketMessageChannel? why do I have to cast to it? 🤔
			if( discordClient.GetChannel( channelId ) is ISocketMessageChannel channel )
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

			foreach( var mentionedUser in msg.MentionedUsers )
			{
				//the string that's used to mention this user, we need to remove the exclamation mark here tho
				String mentionString = mentionedUser.Mention.Replace( "!" , String.Empty );

				//the bot doesn't need to see "DuckBot" at the start of the message everytime, so remove that specifically
				text = text.Replace( mentionString , mentionedUser.Id == discordClient.CurrentUser.Id ? String.Empty : mentionedUser.Username );
			}

			return new Activity()
			{
				Text = text ,
				ChannelId = msg.Channel.Id.ToString() ,
				From = DiscordUserToBotFrameworkAccount( msg.Author ) ,
				Recipient = DiscordUserToBotFrameworkAccount( discordClient.CurrentUser ) ,
				Conversation = new ConversationAccount( true , null , msg.Channel.Id.ToString() , msg.Channel.Name ) ,
				Timestamp = msg.Timestamp ,
				Id = msg.Id.ToString() ,
				Type = ActivityTypes.Message ,
			};
		}

		public ChannelAccount DiscordUserToBotFrameworkAccount( SocketUser user )
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
		public override async Task<ResourceResponse> UpdateActivity( ITurnContext context , Activity activity )
		{
			await Task.CompletedTask;
			return null;
		}

		public override async Task DeleteActivity( ITurnContext context , ConversationReference reference )
		{
			await Task.CompletedTask;
		}
	}
}
