using Google.Apis.Auth.OAuth2;
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


			var data = await DB.GetData<MatchData>( "2018-10-23 22-11-52" );
			data.YoutubeUrl = "LcLkMn3Duw0";
			await DB.SaveData( data );
			/*
			var newVideoData = await UploaderUtils.GetVideoDataForDatabaseItem( DB , data );
			await UploaderUtils.UpdateVideoData( Service , "LcLkMn3Duw0" , newVideoData );
			*/
			//var stuff2 = await UploaderUtils.GetVideoData( Service , "LcLkMn3Duw0" );
			/*
			var res = await FindYoutubeDrafts( new List<IDatabaseEntry>() { data } );
			*/
		}

		private async Task<ResourceId> FindYoutubeDraft( IDatabaseEntry entry ) => await FindYoutubeDraft( entry.DatabaseIndex );

		private async Task<ResourceId> FindYoutubeDraft( string databaseIndex )
		{
			var strippedName = UploaderUtils.GetYoutubeStrippedName( databaseIndex );

			var req = Service.Search.List( "snippet" );
			req.Q = strippedName;
			req.ForMine = true;
			req.MaxResults = 1;
			req.Type = "video";

			var resp = await req.ExecuteAsync();
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
			var testdata = await DB.GetData<MatchData>( "2018-10-23 22-11-52" );
			var testUpload = await CreatePendingUpload( testdata );
			//testUpload.UploadUrl = new Uri( $"https://www.youtube.com/watch?v={await FindYoutubeDraft( testdata )}" );
			Uploads.Enqueue( testUpload );

			return;
			/*
			//try to find any matches that are merged videos and that have a "video.mp4" video in it
			var concMatches = new ConcurrentBag<MatchData>();

			await DB.IterateOverAll<MatchData>( ( matchData ) =>
			{
				if( matchData.VideoType == VideoType.MergedVideoLink && string.IsNullOrEmpty( matchData.YoutubeUrl ) )
				{
					concMatches.Add( matchData );
				}

				return Task.FromResult( true );
			} );

			var matchesList = concMatches.ToList<IDatabaseEntry>();

			var foundVideos = await FindYoutubeDrafts( matchesList );

			foreach( var data in matchesList )
			{
				var pendingUpload = await CreatePendingUpload( data );

				if( pendingUpload != null && foundVideos.TryGetValue( data.DatabaseIndex , out var resource ) )
				{
					pendingUpload.UploadUrl = new Uri( $"https://www.youtube.com/watch?v={resource.VideoId}" );
					Uploads.Enqueue( pendingUpload );
				}
			}
			*/
		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			var matchData = await DB.GetData<MatchData>( upload.DataName );
			var videoResource = await FindYoutubeDraft( matchData );

			return false;
		}
	}
}
