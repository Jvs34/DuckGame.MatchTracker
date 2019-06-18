using Flurl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class HttpGameDatabase : BaseGameDatabase
	{
		protected HttpClient Client { get; }

		public override bool ReadOnly => true;

		public virtual bool InitialLoad { get; set; } = false;

		public HttpGameDatabase( HttpClient httpClient )
		{
			Client = httpClient;
		}

		public override async Task Load()
		{
			if( InitialLoad )
			{
				await LoadZippedDatabase();
			}
		}

		protected virtual async Task<Stream> GetZippedDatabaseStream()
		{
			string repositoryZipUrl = Url.Combine( "https://github.com" , SharedSettings.RepositoryUser , SharedSettings.RepositoryName , "archive" , "master.zip" );

			var httpResponse = await Client.GetAsync( repositoryZipUrl , HttpCompletionOption.ResponseHeadersRead );

			if( httpResponse.IsSuccessStatusCode )
			{
				return await httpResponse.Content.ReadAsStreamAsync();
			}

			return null;
		}

		protected virtual async Task LoadZippedDatabase()
		{
			var zipStream = await GetZippedDatabaseStream();

			if( zipStream != null )
			{
				using( var responseStream = zipStream )
				using( ZipArchive zipArchive = new ZipArchive( responseStream , ZipArchiveMode.Read ) )
				{
					CacheEverythingInZip( zipArchive );
				}
			}
		}

		protected void CacheEverythingInZip( ZipArchive zipArchive )
		{
			DateTime expireTime = DateTime.UtcNow.AddSeconds( 150 );
			//try to get the globaldata
			var globalData = GetZipEntry<GlobalData>( zipArchive );

			SetCachedItem( globalData , expireTime );

			foreach( var matchName in globalData.Matches )
			{
				var matchData = GetZipEntry<MatchData>( zipArchive , matchName );
				SetCachedItem( matchData , expireTime );
			}

			foreach( var roundName in globalData.Rounds )
			{
				var roundData = GetZipEntry<RoundData>( zipArchive , roundName );
				SetCachedItem( roundData , expireTime );
			}

			foreach( var levelName in globalData.Levels )
			{
				var levelData = GetZipEntry<LevelData>( zipArchive , levelName );
				SetCachedItem( levelData , expireTime );
			}

			foreach( var tagName in globalData.Tags )
			{
				var tagData = GetZipEntry<TagData>( zipArchive , tagName );
				SetCachedItem( tagData , expireTime );
			}

		}

		protected string ToZipPath( ZipArchive zipArchive , string path )
		{
			return path.Replace( SharedSettings.BaseRecordingFolder , zipArchive.Entries [0]?.FullName.Replace( "/" , string.Empty ) ).Replace( "\\" , "/" );
		}


		protected T GetZipEntry<T>( ZipArchive zipArchive , string databaseIndex = "" ) where T : IDatabaseEntry
		{
			T data = default;
			var dataPath = ToZipPath( zipArchive , SharedSettings.GetDataPath<T>( databaseIndex ) );

			var zipEntry = zipArchive.GetEntry( dataPath );
			if( zipEntry != null )
			{
				using( var stream = zipEntry.Open() )
				{
					data = Deserialize<T>( stream );
				}
			}

			return data;
		}


		public override async Task SaveData<T>( T data )
		{
			await Task.CompletedTask;
		}

		public override async Task<T> GetData<T>( string dataId = "" )
		{
			T data = GetCachedItem<T>( dataId );

			bool cacheExpired = IsCachedItemExpired<T>( dataId , DateTime.UtcNow );

			string url = SharedSettings.GetDataPath<T>( dataId , true );

			if( !string.IsNullOrEmpty( url ) && cacheExpired )
			{
				try
				{
					var httpResponse = await Client.GetAsync( url , HttpCompletionOption.ResponseHeadersRead );

					if( httpResponse.IsSuccessStatusCode )
					{
						using( var responseStream = await httpResponse.Content.ReadAsStreamAsync() )
						{
							data = Deserialize( responseStream , data );
						}

						SetCachedItem( data , httpResponse.Content.Headers.Expires.HasValue
							? httpResponse.Content.Headers.Expires.Value.DateTime
							: DateTime.UtcNow.AddSeconds( 60 ) );
					}
				}
				catch( HttpRequestException e )
				{
					Console.WriteLine( e );
					System.Diagnostics.Debug.WriteLine( e );
				}
			}

			return (T) data;
		}

	}
}
