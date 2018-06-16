using Discord;
using System;
using Microsoft.Rest;
using System.Threading;

namespace MatchBot
{
	public static class Program
	{
		public static void Main( string [] args )
		{
			DiscordBotHandler handler = new DiscordBotHandler();
			handler.Initialize().Wait();

			Console.WriteLine( "Gay" );
			Console.ReadKey();
		}


	}
}
