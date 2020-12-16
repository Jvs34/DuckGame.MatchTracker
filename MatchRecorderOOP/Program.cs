using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MatchRecorder
{
	class Program
	{
		public static async Task Main( string [] args ) => await CreateHostBuilder( args ).Build().RunAsync();

		public static IHostBuilder CreateHostBuilder( string [] args ) =>
			Host
				.CreateDefaultBuilder( args )
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
