using System;
using Microsoft.Rest;
using System.Threading;

namespace MatchBot
{
	public static class Program
	{
		public static void Main( string [] args )
		{
			try
			{
				DiscordBotHandler handler = new DiscordBotHandler();
				handler.Initialize().Wait();

				Console.WriteLine( "Press a key to fucking stop" );
				Console.ReadKey();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}
		}
	}
}
