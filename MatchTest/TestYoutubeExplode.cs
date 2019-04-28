using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

namespace MatchTest
{
	public class TestYoutubeExplode
	{
		YoutubeClient YTClient { get; } = new YoutubeClient();
		public async Task Test()
		{
			//this is the first one
			
			var video = await YTClient.GetVideoMediaStreamInfosAsync( "SwsisBp6Qaw" );

			foreach( var vid in video.Muxed )
			{
				Console.WriteLine( vid.Url );
				Console.WriteLine();
			}




			await Task.CompletedTask;
		}
	}
}
