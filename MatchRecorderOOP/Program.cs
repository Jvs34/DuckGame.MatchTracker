using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace MatchRecorder
{
	class Program
	{
		public static async Task Main( string [] args )
		{
			var host = CreateHostBuilder( args ).Build();
			await host.InitAsync();
			await host.RunAsync();
		}

		public static IHostBuilder CreateHostBuilder( string [] args ) =>
			Host
				.CreateDefaultBuilder( args )
				.ConfigureAppConfiguration( ( hostingContext , config ) =>
				{
					var path = Directory.GetCurrentDirectory();
					config
#if DEBUG
					.AddJsonFile( Path.Combine( path , "Settings" , "shared_debug.json" ) )
#else
					.AddJsonFile( Path.Combine( path , "Settings" , "shared.json" ) )
#endif
					.AddJsonFile( Path.Combine( path , "Settings" , "bot.json" ) )
					.AddJsonFile( Path.Combine( path , "Settings" , "obs.json" ) )
					.AddJsonFile( Path.Combine( path , "Settings" , "uploader.json" ) )
					.AddCommandLine( args );
				} )
				.ConfigureLogging( logging =>
				{
					logging.ClearProviders();
					logging.AddConsole();
				} )
				.ConfigureWebHostDefaults( webBuilder =>
				{
					webBuilder.UseUrls( "http://localhost:6969/" );
					webBuilder.UseStartup<Startup>();
				} );
	}
}
