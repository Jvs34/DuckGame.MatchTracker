using LiteDB;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MatchShared.Databases.LiteDB;

public class LiteDBGameDatabase : BaseGameDatabase, IDisposable
{
	public override bool IsReadOnly { get; }
	public LiteDatabase Database { get; protected set; }
	protected BsonMapper Mapper { get; } = new BsonMapper();

	public LiteDBGameDatabase() : this( false ) { }
	public LiteDBGameDatabase( bool readOnly ) => IsReadOnly = readOnly;

	protected override void DefineMappingInternal<T>() => Mapper.Entity<T>().Id( x => x.DatabaseIndex );

	public override Task Load( CancellationToken token = default )
	{
		if( IsLoaded )
		{
			return Task.CompletedTask;
		}

		var connectionString = new ConnectionString
		{
			Connection = ConnectionType.Direct,
			Filename = SharedSettings.GetDatabasePath(),
			ReadOnly = IsReadOnly,
		};

		Database = new LiteDatabase( connectionString, Mapper );
		IsLoadedInternal = true;
		return Task.CompletedTask;
	}

	public override Task<T> GetData<T>( string dataId = "", CancellationToken token = default )
	{
		if( string.IsNullOrEmpty( dataId ) )
		{
			dataId = typeof( T ).Name;
		}

		var collection = Database.GetCollection<T>( typeof( T ).Name );

		T data = collection.FindById( dataId );
		return Task.FromResult( data );
	}

	public override Task<bool> SaveData<T>( T data, CancellationToken token = default )
	{
		var collection = Database.GetCollection<T>( data.GetType().Name );
		collection.Upsert( data );
		return Task.FromResult( true );
	}

	public override Task<bool> SaveData<T>( IEnumerable<T> datas, CancellationToken token = default )
	{
		var collection = Database.GetCollection<T>( typeof( T ).Name );
		collection.Upsert( datas );
		return Task.FromResult( true );
	}

	protected override void InternalDispose()
	{
		Database?.Dispose();
		Database = null;
	}
}
