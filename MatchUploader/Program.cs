using System;
using System.Threading.Tasks;

namespace MatchUploader
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{
			MatchUploaderHandler mu = new MatchUploaderHandler();

			try
			{
				await mu.Run();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			Console.WriteLine( "Program either had an exception or it's done working" );
			Console.ReadKey();
		}
	}
}
