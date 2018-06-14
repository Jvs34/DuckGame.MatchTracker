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
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Rest;

namespace MatchBot
{
	public class DiscordHandler
	{
		private class LocalCredentials : ServiceClientCredentials
		{


			public override Task ProcessHttpRequestAsync( HttpRequestMessage request , CancellationToken cancellationToken )
			{
				Console.WriteLine( request.RequestUri );
				request.Headers.Authorization = new AuthenticationHeaderValue( "Bearer" , "" );
				return base.ProcessHttpRequestAsync( request , cancellationToken );
			}
		}


		private DiscordSocketClient discordClient;
		private ConnectorClient microsoftBotClient;

		public DiscordHandler()
		{
			discordClient = new DiscordSocketClient();
			discordClient.Connected += OnDiscordConnected;
			discordClient.Disconnected += OnDiscordDisconnected;
			//client.LoginAsync( TokenType.Bot , "" );

			microsoftBotClient = new ConnectorClient( new Uri( "https://localhost" ) );
			
		}

		private Task OnDiscordDisconnected( Exception arg )
		{
			throw new NotImplementedException();
		}

		private Task OnDiscordConnected()
		{
			throw new NotImplementedException();
		}
	}
}
