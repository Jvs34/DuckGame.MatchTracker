using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Discord;
using Microsoft.Bot.Connector.DirectLine;
using System;
using Microsoft.Rest;

namespace MatchBot
{
	public static class Program
	{
		public static void Main( string [] args )
		{
			DiscordHandler handler = new DiscordHandler();

			BuildWebHost( args ).RunAsync().Wait();
		}

		public static IWebHost BuildWebHost( string [] args ) =>
			WebHost.CreateDefaultBuilder( args )
				.UseStartup<Startup>()
				.Build();
	}
}
