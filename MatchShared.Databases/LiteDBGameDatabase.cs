﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class LiteDBGameDatabase : IGameDatabase, IDisposable
	{
		private bool disposedValue;

		public SharedSettings SharedSettings { get; set; } = new SharedSettings();
		public bool ReadOnly => false;
		public LiteDatabase Database { get; protected set; }
		protected BsonMapper Mapper { get; } = new BsonMapper();

		public LiteDBGameDatabase()
		{
			DefineMapping<IDatabaseEntry>();
			DefineMapping<EntryListData>();
			DefineMapping<RoundData>();
			DefineMapping<MatchData>();
			DefineMapping<LevelData>();
			DefineMapping<TagData>();
			DefineMapping<PlayerData>();
		}

		protected void DefineMapping<T>() where T : IDatabaseEntry => Mapper.Entity<T>().Id( x => x.DatabaseIndex );

		public Task Load( CancellationToken token = default )
		{
			Database = new LiteDatabase( SharedSettings.GetDatabasePath() , Mapper );
			return Task.CompletedTask;
		}

		public Task<T> GetData<T>( string dataId = "" , CancellationToken token = default ) where T : IDatabaseEntry
		{
			if( string.IsNullOrEmpty( dataId ) )
			{
				dataId = typeof( T ).Name;
			}

			var collection = Database.GetCollection<T>( typeof( T ).Name );

			T data = collection.FindById( dataId );
			return Task.FromResult( data );
		}

		public Task SaveData<T>( T data , CancellationToken token = default ) where T : IDatabaseEntry
		{
			var collection = Database.GetCollection<T>( data.GetType().Name );
			collection.Upsert( data );
			return Task.CompletedTask;
		}

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					Database?.Dispose();
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}
