using System;

namespace MatchUploader
{
	class Program
	{
		static void Main( string [] args )
		{
			MatchManager mm = new MatchManager();
			try
			{
				mm.UpdateGlobalData( @"E:\DebugGameRecordings" );
			}
			catch( Exception e )
			{
				Console.WriteLine( e.ToString() );
			}
			Console.WriteLine( "Program either had an exception or it's done working" );
			Console.ReadKey();
		}
	}
}
