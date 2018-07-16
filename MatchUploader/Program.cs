using System;
using System.Threading.Tasks;

namespace MatchUploader
{
	static class Program
	{
		static async Task Main( string [] args )
		{
			MatchUploaderHandler mu = new MatchUploaderHandler();

			try
			{
				await mu.UpdateGlobalData();
				await mu.DoYoutubeLoginAsync();
				mu.SaveSettings();
				await mu.CleanupVideos();
				mu.CommitGitChanges();
				await mu.UpdatePlaylists();
				await mu.UploadAllRounds();
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
