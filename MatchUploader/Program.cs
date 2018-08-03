using System;
using System.Threading.Tasks;

namespace MatchUploader
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{
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