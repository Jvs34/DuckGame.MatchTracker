using System;

namespace MatchBot
{
	public static class Program
	{
		public static void Main( string [] args )
		{
			try
			{
				DiscordBotHandler handler = new DiscordBotHandler( args );
				handler.Initialize().Wait();

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
