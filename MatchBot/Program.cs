using System;
using System.Threading.Tasks;

namespace MatchBot
{
	public static class Program
	{
		public static async Task Main( string [] args )
		{
			try
			{
				DiscordBotHandler handler = new DiscordBotHandler( args );
				await handler.Initialize();

				Console.WriteLine( "Press a key to stop" );
				Console.ReadKey();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}
		}
	}
}