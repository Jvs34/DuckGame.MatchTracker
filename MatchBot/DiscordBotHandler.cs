using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MatchTracker;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchBot
{
	public class DiscordBotHandler : BotAdapter
	{
		private IBot bot;
		private BotSettings botSettings;
		private DiscordClient discordClient;
		private IConfigurationRoot Configuration { get; }

		public DiscordBotHandler( string [] args )
		{
			botSettings = new BotSettings();
			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "bot.json" )
				.AddCommandLine( args )
			.Build();

			Configuration.Bind( botSettings );

			discordClient = new DiscordClient( new DiscordConfiguration()
			{
				AutoReconnect = true ,
				TokenType = TokenType.Bot ,
				Token = botSettings.DiscordToken ,
			} );

			discordClient.SocketOpened += OnDiscordConnected;
			discordClient.SocketClosed += OnDiscordDisconnected;
			discordClient.MessageCreated += OnDiscordMessage;
			discordClient.MessageReactionAdded += OnDiscordMessageReactionAdded;
			discordClient.MessageReactionRemoved += OnDiscordMessageReactionRemoved;

			bot = new MatchBot( Configuration );
		}



		public override async Task DeleteActivityAsync( ITurnContext context , ConversationReference reference , CancellationToken cancellationToken )
		{
			await Task.CompletedTask;
		}

		public ChannelAccount DiscordUserToBotFrameworkAccount( DiscordUser user )
		{
			return new ChannelAccount()
			{
				Id = user.Id.ToString() ,
				Name = user.Username ,
				Role = user.IsBot ? RoleTypes.Bot : RoleTypes.User
			};
		}

		public async Task Initialize()
		{
			await discordClient.ConnectAsync();
			await discordClient.InitializeAsync();
			await discordClient.UpdateStatusAsync( null , UserStatus.Online );
		}

		public override async Task<ResourceResponse []> SendActivitiesAsync( ITurnContext context , Activity [] activities , CancellationToken cancellationToken )
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
		public override async Task<ResourceResponse> UpdateActivityAsync( ITurnContext turnContext , Activity activity , CancellationToken cancellationToken )
		{
			

			await Task.CompletedTask;
			return null;
		}

		private async Task BotCallback( ITurnContext context , CancellationToken cancellationToken )
		{
			await bot.OnTurnAsync( context );
		}

		private Activity GetActivityFromMessage( MessageCreateEventArgs msg )
		{
			string text = msg.Message.Content;

			foreach( var mentionedUser in msg.MentionedUsers )
			{
				//the string that's used to mention this user, we need to remove the exclamation mark here tho
				string mentionString = mentionedUser.Mention.Replace( "!" , string.Empty );

				//the bot doesn't need to see "DuckBot" at the start of the message everytime, so remove that specifically
				text = text.Replace( mentionString , mentionedUser.Id == discordClient.CurrentUser.Id ? string.Empty : mentionedUser.Username );
			}

			return new Activity()
			{
				Text = text ,
				ChannelId = msg.Channel.Id.ToString() ,
				From = DiscordUserToBotFrameworkAccount( msg.Author ) ,
				Recipient = DiscordUserToBotFrameworkAccount( discordClient.CurrentUser ) ,
				Conversation = new ConversationAccount( true , null , msg.Channel.Id.ToString() , msg.Channel.Name ) ,
				Timestamp = msg.Message.Timestamp ,
				Id = msg.Message.Id.ToString() ,
				Type = ActivityTypes.Message ,
			};
		}


		//message incoming from discord, ignoring the ones from the bot
		private async Task HandleIncomingMessage( MessageCreateEventArgs msg )
		{
			Activity act = GetActivityFromMessage( msg );
			using( var context = new TurnContext( this , act ) )
			{
				await RunPipelineAsync( context , BotCallback , CancellationToken.None );
			}
		}

		//message outgoing from the bot
		private async Task HandleOutgoingMessage( ITurnContext context , Activity act )
		{
			//get the channel the original message came from
			ulong channelId = Convert.ToUInt64( act.ChannelId );

			var channel = await discordClient.GetChannelAsync( channelId );
			if( channel != null )
			{

				DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder();

				if( act.Attachments != null )
				{
					foreach( var attachment in act.Attachments )
					{

						//attachment.ContentUrl;
					}
				}
				await channel.SendMessageAsync( act.Text , false /*, discordEmbedBuilder.Build()*/ );
			}
		}

		private async Task OnDiscordConnected()
		{
			await Task.CompletedTask;
			Console.WriteLine( "Connected to Discord" );
		}

		private async Task OnDiscordDisconnected( SocketCloseEventArgs arg )
		{
			await Task.CompletedTask;
			Console.WriteLine( "Disconnected from Discord {0}" , arg.CloseMessage );
		}

		private async Task OnDiscordMessage( MessageCreateEventArgs msg )
		{
			//there's no equality check on this class unfortunately
			if( msg.Author.Id == discordClient.CurrentUser.Id )
				return;

			//we don't care about messages that are not sent to us privately or that aren't mentioning us
			if( msg.Channel.Type != ChannelType.Private && !msg.MentionedUsers.Any( x => x.Id == discordClient.CurrentUser.Id ) )
				return;

			await HandleIncomingMessage( msg );
		}


		//
		private async Task OnDiscordMessageReactionAdded( MessageReactionAddEventArgs e )
		{
			await Task.CompletedTask;

		}

		private async Task OnDiscordMessageReactionRemoved( MessageReactionRemoveEventArgs e )
		{
			await Task.CompletedTask;

			Activity activity = new Activity();
		}
	}
}