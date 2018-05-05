using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;
namespace MatchUploader
{
	class Program
	{
		static void Main( string [] args )
		{
			MatchUploaderHandler mu = new MatchUploaderHandler();
			
			try
			{
				
				mu.UpdateGlobalData();
				mu.DoYoutubeLoginAsync().Wait();
				mu.SaveSettings();

				//mu.UploadRoundToYoutubeAsync( "2018-04-28 19-55-08" ).Wait();
				mu.CleanupVideos();
				mu.UploadAllRounds().Wait();


			}
			catch( Exception e )
			{

			}
			
			Console.WriteLine( "Program either had an exception or it's done working" );
			Console.ReadKey();
		}
	}
}
