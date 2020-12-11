using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MatchUploader
{
	public static class Program
	{
		public static async Task Main( string [] args )
		{
			//basically, on linux or specifically debian the IDN resolve fucks up something fierce, so we need to set this to
			//make YoutubeExplode work properly
			if( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
			{
				AppContext.SetSwitch( "System.Net.Http.UseSocketsHttpHandler" , false );
			}

			var uploader = new MatchUploaderHandler( args );

			await uploader.RunAsync();
		}
	}
}