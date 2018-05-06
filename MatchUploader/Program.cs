using System;
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
				mu.CleanupVideos();
				mu.CommitGitChanges();
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
