using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MatchTracker;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchUploader
{
	internal static class UploaderUtils
	{
		/// <summary>
		/// Get all the winners as a merged string
		/// </summary>
		/// <param name="data"></param>
		/// <param name="DB"></param>
		/// <returns>EG: "Willox Jvs"</returns>
		internal static async Task<string> GetAllWinners( IGameDatabase DB , IWinner data )
		{
			string winners = string.Empty;
			var playerWinners = await DB.GetAllData<PlayerData>( data.GetWinners() );
			winners = string.Join( " " , playerWinners.Select( x => x.GetName() ) );

			if( string.IsNullOrEmpty( winners ) )
			{
				winners = "Nobody";
			}

			return winners;
		}

		/// <summary>
		/// Youtube mostly strips out dashes from the name, that's about it
		/// </summary>
		/// <param name="databaseIndex"></param>
		/// <returns></returns>
		public static string GetYoutubeStrippedName( string databaseIndex )
		{
			return databaseIndex.Replace( '-' , ' ' );
		}

		internal static async Task<Video> GetVideoDataForDatabaseItem<T>( IGameDatabase DB , T data ) where T : IPlayersList, IStartEnd, IWinner, IDatabaseEntry, IVideoUpload
		{
			string mainWinners = await GetAllWinners( DB , data );
			string description = string.Empty;
			string privacystatus = "unlisted";

			if( data.VideoType == VideoType.VideoLink )
			{
				description = $"Recorded on {DB.SharedSettings.DateTimeToString( data.TimeStarted )}\nThe winner is {mainWinners}";
			}

			//only update the description for the main video
			if( data.VideoType == VideoType.MergedVideoLink && data is MatchData matchData )
			{
				privacystatus = "public";

				var builder = new StringBuilder();
				builder
					.Append( $"Recorded on {DB.SharedSettings.DateTimeToString( data.TimeStarted )}" )
					.AppendLine()
					.Append( $"The winner is {mainWinners}" )
					.AppendLine()
					.AppendLine()
					.Append( $"Rounds: {matchData.Rounds.Count}" )
					.AppendLine();

				//now add the rounds down here so that the whole youtube chaptering thing works

				foreach( var roundName in matchData.Rounds )
				{
					var roundData = await DB.GetData<RoundData>( roundName );
					builder
						.Append( $"{roundData.VideoStartTime:mm\\:ss} - {data.DatabaseIndex} {await GetAllWinners( DB , roundData ) }" )
						.AppendLine();
				}

				description = builder.ToString();
			}

			Video videoData = new Video()
			{
				Snippet = new VideoSnippet()
				{
					Title = $"{data.DatabaseIndex} {mainWinners}" ,
					Tags = new List<string>() { "duckgame" , "peniscorp" } ,
					CategoryId = "20" ,
					Description = description ,
				} ,
				Status = new VideoStatus()
				{
					PrivacyStatus = privacystatus ,
					MadeForKids = false ,
				} ,
				RecordingDetails = new VideoRecordingDetails()
				{
					RecordingDate = data.TimeStarted.ToString( "s" , CultureInfo.InvariantCulture ) ,
				}
			};

			return videoData;
		}

		internal static async Task<Video> GetVideoData( YouTubeService service , string youtubeId )
		{
			var req = service.Videos.List( new string [] { "snippet" , "status" , "processingDetails" } );
			req.Id = youtubeId;

			var resp = await req.ExecuteAsync();
			return resp.Items.FirstOrDefault();
		}

		internal static async Task UpdateVideoData( YouTubeService service , string youtubeId , Video videoData )
		{
			videoData.Id = youtubeId;
			var req = service.Videos.Update( videoData , new string [] { "snippet" , "status" , "recordingDetails" } );

			await req.ExecuteAsync();
		}
	}
}
