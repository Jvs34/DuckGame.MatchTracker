﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MatchTracker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchUploader
{
	/// <summary>
	/// Not exactly an uploader, you have to upload the videos yourself with the youtube interface, then run this to
	/// link up the video to the database and delete dangling videos that were uploaded
	/// </summary>
	public class YoutubeMatchUpdater : Uploader
	{
		public YouTubeService Service { get; private set; }

		public YoutubeMatchUpdater( IGameDatabase gameDatabase , UploaderSettings settings ) : base( gameDatabase , settings )
		{
		}

		public override async Task Initialize()
		{
			string appName = GetType().Assembly.GetName().Name;
			Service = new YouTubeService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = await GoogleWebAuthorizationBroker.AuthorizeAsync( UploaderSettings.GoogleSecrets ,
					new [] {
						YouTubeService.Scope.Youtube
					} ,
					"youtube" ,
					CancellationToken.None ,
					UploaderSettings.GoogleDataStore
				) ,
				ApplicationName = appName ,
				GZipEnabled = true ,
			} );
			Service.HttpClient.Timeout = TimeSpan.FromMinutes( 2 );

			if( !Directory.Exists( Path.Combine( Path.GetTempPath() , "MatchUploader" ) ) )
			{
				Directory.CreateDirectory( Path.Combine( Path.GetTempPath() , "MatchUploader" ) );
			}
		}

		private async Task<ResourceId> FindYoutubeDraft( IDatabaseEntry entry ) => await FindYoutubeDraft( entry.DatabaseIndex );

		private async Task<ResourceId> FindYoutubeDraft( string databaseIndex )
		{
			var strippedName = UploaderUtils.GetYoutubeStrippedName( databaseIndex );

			var req = Service.Search.List( "snippet" );
			req.Q = strippedName;
			req.ForMine = true;
			req.MaxResults = 50; //max results to 50, just in case there might be a couple of duplicates
			req.Type = "video";

			var resp = await req.ExecuteAsync();
			//duplicate videos do not have a snippet, so filter them out
			return resp.Items.FirstOrDefault( item => item.Snippet != null && strippedName == item.Snippet.Title )?.Id;
		}

		private async Task<Dictionary<string , ResourceId>> FindYoutubeDrafts( List<IDatabaseEntry> databaseEntries )
		{
			var resources = new Dictionary<string , ResourceId>();

			var pageToken = string.Empty;

			while( pageToken != null )
			{
				var req = Service.Search.List( "snippet" );
				req.Q = string.Empty;
				req.ForMine = true;
				req.PageToken = pageToken;
				req.MaxResults = 50;
				req.Type = "video";

				var resp = await req.ExecuteAsync();

				foreach( var item in resp.Items )
				{
					var dataEntry = databaseEntries.FirstOrDefault( data => item.Snippet != null && data.DatabaseIndex.Replace( '-' , ' ' ) == item.Snippet.Title );

					if( dataEntry != null )
					{
						resources.Add( dataEntry.DatabaseIndex , item.Id );
					}
				}

				pageToken = resp.NextPageToken;

			}

			return resources;
		}

		protected override Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry )
		{
			return Task.FromResult( new PendingUpload()
			{
				DataName = entry.DatabaseIndex ,
				DataType = entry.GetType().Name ,
			} );
		}

		protected override async Task FetchUploads()
		{
			//try to find any matches that are merged videos and that have a "video.mp4" video in it
			var concMatches = new ConcurrentBag<MatchData>();

			var toCleanup = new ConcurrentBag<MatchData>();

			await DB.IterateOverAll<MatchData>( ( matchData ) =>
			{
				if( matchData.VideoType == VideoType.MergedVideoLink )
				{
					if( string.IsNullOrEmpty( matchData.YoutubeUrl ) )
					{
						concMatches.Add( matchData );
					}
					else
					{
						if( File.Exists( DB.SharedSettings.GetMatchVideoPath( matchData.DatabaseIndex ) ) )
						{
							toCleanup.Add( matchData );
						}
					}
				}

				return Task.FromResult( true );
			} );

			foreach( var data in toCleanup )
			{
				if( File.Exists( DB.SharedSettings.GetMatchVideoPath( data.DatabaseIndex ) ) )
				{
					File.Delete( DB.SharedSettings.GetMatchVideoPath( data.DatabaseIndex ) );
				}

				//now check the temp folder too

				if( File.Exists( Path.Combine( Path.GetTempPath() , "MatchUploader" , $"{data.DatabaseIndex}.mp4" ) ) )
				{
					File.Delete( Path.Combine( Path.GetTempPath() , "MatchUploader" , $"{data.DatabaseIndex}.mp4" ) );
				}
			}

			foreach( var data in concMatches )
			{
				var pendingUpload = await CreatePendingUpload( data );

				if( pendingUpload != null )
				{
					Uploads.Enqueue( pendingUpload );

					//now add the file to the temp path if it doesn't exist
					if( !File.Exists( Path.Combine( Path.GetTempPath() , "MatchUploader" , $"{data.DatabaseIndex}.mp4" ) ) )
					{
						File.Copy( DB.SharedSettings.GetMatchVideoPath( data.DatabaseIndex ) , Path.Combine( Path.GetTempPath() , "MatchUploader" , $"{data.DatabaseIndex}.mp4" ) );
					}
				}
			}
		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			var matchData = await DB.GetData<MatchData>( upload.DataName );
			var videoResource = await FindYoutubeDraft( matchData );

			if( videoResource is null )
			{
				return false;
			}

			//now override the match data to publish the item
			try
			{
				matchData.YoutubeUrl = videoResource.VideoId;
				var matchVideoData = await UploaderUtils.GetVideoDataForDatabaseItem( DB , matchData );
				await UploaderUtils.UpdateVideoData( Service , videoResource.VideoId , matchVideoData );
				await DB.SaveData( matchData );

				//also update the youtubeurl on all the linked rounds

				await DB.IterateOver<RoundData>( async ( roundData ) =>
				{
					if( roundData.VideoType == VideoType.MergedVideoLink && string.IsNullOrEmpty( roundData.YoutubeUrl ) )
					{
						roundData.YoutubeUrl = videoResource.VideoId;
						await DB.SaveData( roundData );
					}

					return true;
				} , matchData.Rounds );


				//now delete the file in the temp folder and in the normal path
				if( File.Exists( DB.SharedSettings.GetMatchVideoPath( matchData.DatabaseIndex ) ) )
				{
					File.Delete( DB.SharedSettings.GetMatchVideoPath( matchData.DatabaseIndex ) );
				}

				//now check the temp folder too

				if( File.Exists( Path.Combine( Path.GetTempPath() , "MatchUploader" , $"{matchData.DatabaseIndex}.mp4" ) ) )
				{
					File.Delete( Path.Combine( Path.GetTempPath() , "MatchUploader" , $"{matchData.DatabaseIndex}.mp4" ) );
				}

				return true;
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			return false;
		}
	}
}
