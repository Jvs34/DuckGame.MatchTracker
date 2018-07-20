using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchUploader
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{
			Parser.Default.ParseArguments<CommandLineOptions>( args ).WithParsed( RunOptionsAndReturnExitCode );

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

		private static void RunOptionsAndReturnExitCode( CommandLineOptions opts )
		{
			Console.WriteLine( "Gay" );
			//throw new NotImplementedException();
		}
	}
}
