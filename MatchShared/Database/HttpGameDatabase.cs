using Flurl;
using System;
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
			//TODO: unhardcode this to not use github
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

			var playersEntry = GetZipEntry<EntryListData>( zipArchive , nameof( PlayerData ) );
			if( playersEntry != null )
			{
				SetCachedItem( playersEntry , expireTime );
				foreach( var playerName in playersEntry.Entries )
				{
					SetCachedItem( GetZipEntry<PlayerData>( zipArchive , playerName ) , expireTime );
				}
			}

			var matchesEntry = GetZipEntry<EntryListData>( zipArchive , nameof( MatchData ) );
			if( matchesEntry != null )
			{
				SetCachedItem( matchesEntry , expireTime );
				foreach( var matchName in matchesEntry.Entries )
				{
					SetCachedItem( GetZipEntry<MatchData>( zipArchive , matchName ) , expireTime );
				}
			}

			var roundsEntry = GetZipEntry<EntryListData>( zipArchive , nameof( RoundData ) );
			if( roundsEntry != null )
			{
				SetCachedItem( roundsEntry , expireTime );
				foreach( var roundName in roundsEntry.Entries )
				{
					SetCachedItem( GetZipEntry<RoundData>( zipArchive , roundName ) , expireTime );
				}
			}

			var levelsEntry = GetZipEntry<EntryListData>( zipArchive , nameof( LevelData ) );
			if( levelsEntry != null )
			{
				SetCachedItem( levelsEntry , expireTime );
				foreach( var levelName in levelsEntry.Entries )
				{
					SetCachedItem( GetZipEntry<LevelData>( zipArchive , levelName ) , expireTime );
				}
			}

			var tagsEntry = GetZipEntry<EntryListData>( zipArchive , nameof( TagData ) );
			if( tagsEntry != null )
			{
				SetCachedItem( tagsEntry , expireTime );
				foreach( var tagName in tagsEntry.Entries )
				{
					SetCachedItem( GetZipEntry<TagData>( zipArchive , tagName ) , expireTime );
				}
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
