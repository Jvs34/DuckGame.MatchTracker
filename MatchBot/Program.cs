using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatchBot
{
	public static class Program
	{
		public static async Task Main( string [] args )
		{
			/*
			var hostBuilder = new HostBuilder();

			hostBuilder
				.UseContentRoot( Directory.GetCurrentDirectory() )
				.ConfigureAppConfiguration( config =>
				{
					config.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) );
					config.AddJsonFile( "shared.json" );
					config.AddJsonFile( "bot.json" );
					config.AddJsonFile( "uploader.json" );
					config.AddCommandLine( args );
				} )
				.ConfigureLogging( config =>
				{
					config.AddConsole();
					config.AddDebug();
					config.AddEventSourceLogger();
				} )
				.UseConsoleLifetime();

			*/


			try
			{
				DiscordBotHandler handler = new DiscordBotHandler( args );
				await handler.Initialize();

				Console.WriteLine( "Press a key to stop" );
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}
			Console.ReadLine();
		}
	}
}