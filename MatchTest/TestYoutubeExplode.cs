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
			var video = await YTClient.GetVideoMediaStreamInfosAsync( "j_fkmRJKIpc" );
			var highestQuality = video.Muxed.WithHighestVideoQuality();

			//YTClient.DownloadMediaStreamAsync()
			await Task.CompletedTask;
		}
	}
}
