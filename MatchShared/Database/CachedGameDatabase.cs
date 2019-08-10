using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public abstract class CachedGameDatabase : BaseGameDatabase
	{
		protected Dictionary<string , Dictionary<string , IDatabaseEntry>> Cache { get; } = new Dictionary<string , Dictionary<string , IDatabaseEntry>>();
		protected Dictionary<string , Dictionary<string , DateTime>> CacheExpireTime { get; } = new Dictionary<string , Dictionary<string , DateTime>>();
		protected int DefaultCacheExpireTime { get; set; } = 300;

		protected void CacheEverythingInZip( ZipArchive zipArchive )
		{
			DateTime expireTime = DateTime.UtcNow.AddSeconds( DefaultCacheExpireTime );

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

		protected override async Task LoadEverything()
		{
			await LoadZippedDatabase();
		}

		protected abstract Task<Stream> GetZippedDatabaseStream();

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

		protected string ToZipPath( ZipArchive zipArchive , string path )
		{
			return path
				.Replace( SharedSettings.BaseRecordingFolder , zipArchive.Entries [0]?.FullName )
				.Replace( "\\" , "/" )
				.Replace( "//" , "/" );
		}

		#region CACHE
		protected void ClearCache()
		{
			lock( Cache )
			{
				lock( CacheExpireTime )
				{
					Cache.Clear();
					CacheExpireTime.Clear();
				}
			}
		}

		protected void ClearExpiredCache()
		{
			//TODO
		}

		protected T GetCachedItem<T>( string databaseIndex )
		{

			lock( Cache )
			{
				if( Cache.TryGetValue( typeof( T ).Name , out var keyValues ) && keyValues.TryGetValue( databaseIndex , out var data ) )
				{
					return (T) data;
				}
			}

			return default;
		}

		protected void SetCachedItem<T>( T data , DateTime expire ) where T : IDatabaseEntry
		{
			string typeName = typeof( T ).Name;

			lock( Cache )
			{
				lock( CacheExpireTime )
				{
					if( !Cache.ContainsKey( typeName ) )
					{
						Cache [typeName] = new Dictionary<string , IDatabaseEntry>();
					}

					if( !CacheExpireTime.ContainsKey( typeName ) )
					{
						CacheExpireTime [typeName] = new Dictionary<string , DateTime>();
					}

					Cache [typeName] [data.DatabaseIndex] = data;
					CacheExpireTime [typeName] [data.DatabaseIndex] = expire;
				}
			}
		}

		protected bool IsCachedItemExpired<T>( string databaseIndex , DateTime checkedAgainst ) where T : IDatabaseEntry
		{

			lock( CacheExpireTime )
			{
				if( CacheExpireTime.TryGetValue( typeof( T ).Name , out var keyValues ) && keyValues.TryGetValue( databaseIndex , out var date ) )
				{
					return date < checkedAgainst;
				}
			}

			return true;
		}
		#endregion
	}
}
