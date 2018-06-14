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
				//mu.CleanPlaylists().Wait();
				mu.UpdatePlaylists().Wait();
				mu.UploadAllRounds().Wait();
				mu.UpdatePlaylists().Wait();
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
