using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
namespace MatchUploader
{
	class Program
	{
		static void Main( string [] args )
		{
			MatchUploaderHandler mu = new MatchUploaderHandler();
			mu.DoYoutubeLoginAsync().Wait();
			mu.SaveSettings();

			mu.UploadRoundToYoutubeAsync( "2018-04-30 11-47-24" ).Wait();

			/*
			MatchTracker.PlayerData pd1 = new MatchTracker.PlayerData()
			{
				userId = "69"
			};

			MatchTracker.PlayerData pd2 = new MatchTracker.PlayerData()
			{
				userId = "69"
			};

			Console.WriteLine( pd1.Equals( pd2 ) );
			*/
			//mu.UpdateGlobalData();

			/*
			String path = Path.GetFullPath( Path.Combine( AppContext.BaseDirectory , "..\\..\\..\\..\\" ) );
			MatchTracker.SharedSettings serializ = new MatchTracker.SharedSettings()
			{
				baseRecordingFolder = @"E:\DuckGameRecordings" ,
				debugBaseRecordingFolder = @"E:\DebugGameRecordings"
			};

			File.WriteAllText( Path.Combine( path , "Settings\\shared.json" ) , JsonConvert.SerializeObject( serializ , Formatting.Indented ) );
			*/

			/*
			MatchUploaderHandler mm = new MatchUploaderHandler( @"E:\DebugGameRecordings" );

			try
			{
				mm.UpdateGlobalData();
				mm.UploadRoundToYoutubeAsync( "2018-04-30 11-47-24" ).Wait(); //Willox:that box actually saved you
			}
			catch( Exception e )
			{
				Console.WriteLine( e.ToString() );
			}
			*/

			Console.WriteLine( "Program either had an exception or it's done working" );
			Console.ReadKey();
		}
	}
}
