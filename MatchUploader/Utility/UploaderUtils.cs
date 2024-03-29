﻿using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MatchShared.Databases.Extensions;
using MatchShared.Databases.Interfaces;
using MatchShared.DataClasses;
using MatchShared.Enums;
using MatchShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchUploader.Utility;

internal static class UploaderUtils
{
	/// <summary>
	/// Get all the winners as a merged string
	/// </summary>
	/// <param name="data"></param>
	/// <param name="DB"></param>
	/// <returns>EG: "Willox Jvs"</returns>
	internal static async Task<string> GetAllWinners( IGameDatabase DB, IWinner data )
	{
		var stringBuilder = new StringBuilder();

		var winners = data.GetWinners();

		if( winners.Count == 0 )
		{
			stringBuilder.Append( "Nobody" );
		}
		else
		{
			foreach( var winnerIndex in winners )
			{
				var playerData = await DB.GetData<PlayerData>( winnerIndex );
				if( playerData is null )
				{
					continue;
				}

				stringBuilder
					.Append( playerData.GetName() ?? string.Empty )
					.Append( ' ' );
			}
		}

		return stringBuilder.ToString();
	}

	/// <summary>
	/// Youtube mostly strips out dashes from the name, that's about it
	/// </summary>
	/// <param name="databaseIndex"></param>
	/// <returns></returns>
	public static string GetYoutubeStrippedName( string databaseIndex )
	{
		return databaseIndex.Replace( '-', ' ' );
	}

	internal static async Task<Video> GetVideoDataForDatabaseItem<T>( IGameDatabase DB, T data, VideoUpload upload ) where T : IPlayersList, IStartEndTime, IWinner, IDatabaseEntry
	{
		string mainWinners = await GetAllWinners( DB, data );
		string description = string.Empty;
		string privacystatus = "unlisted";

		if( upload.VideoType == VideoUrlType.VideoLink )
		{
			description = $"Recorded on {DB.SharedSettings.DateTimeToString( data.TimeStarted )}\nThe winner is {mainWinners}";
		}

		//only update the description for the main video
		if( upload.VideoType == VideoUrlType.MergedVideoLink && data is MatchData matchData )
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

				//TODO: properly calculate the chapters again
				TimeSpan timeSpan = TimeSpan.Zero;

				builder
					.Append( $"{timeSpan:mm\\:ss} - {data.DatabaseIndex} {await GetAllWinners( DB, roundData )}" )
					.AppendLine();
			}

			description = builder.ToString();
		}

		var videoData = new Video()
		{
			Snippet = new VideoSnippet()
			{
				Title = $"{data.DatabaseIndex} {mainWinners}",
				Tags = new List<string>() { "duckgame", "peniscorp" },
				CategoryId = "20",
				Description = description,
			},
			Status = new VideoStatus()
			{
				PrivacyStatus = privacystatus,
				MadeForKids = false,
			},
			RecordingDetails = new VideoRecordingDetails()
			{
				RecordingDateDateTimeOffset = data.TimeStarted,
			}
		};

		return videoData;
	}

	internal static async Task<Video> GetVideoData( YouTubeService service, string youtubeId )
	{
		var req = service.Videos.List( new string[] { "snippet", "status", "processingDetails" } );
		req.Id = youtubeId;

		var resp = await req.ExecuteAsync();
		return resp.Items.FirstOrDefault();
	}

	internal static async Task UpdateVideoData( YouTubeService service, string youtubeId, Video videoData )
	{
		videoData.Id = youtubeId;
		var req = service.Videos.Update( videoData, new string[] { "snippet", "status", "recordingDetails" } );

		await req.ExecuteAsync();
	}
}
