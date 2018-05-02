using System;

namespace MatchUploader
{
	class Program
	{
		static void Main( string [] args )
		{
			MatchManager mm = new MatchManager( @"E:\DebugGameRecordings" );

			try
			{
				mm.UpdateGlobalData();
				mm.UploadRoundToYoutube( "2018-04-30 11-47-24" ); //Willox:that box actually saved you
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
