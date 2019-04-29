using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MatchUploader
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{
			//basically, on linux or specifically debian the IDN resolve fucks up something fierce, so we need to set this to
			//make YoutubeExplode work properly
			if( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
			{
				AppContext.SetSwitch( "System.Net.Http.UseSocketsHttpHandler" , false );
			}

			MatchUploaderHandler mu = new MatchUploaderHandler( args );

			try
			{
				await mu.Run();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			Console.WriteLine( "Press a key to stop" );
			Console.ReadKey();
		}
	}
}