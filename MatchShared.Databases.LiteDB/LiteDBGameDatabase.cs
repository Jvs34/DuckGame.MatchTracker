using LiteDB;
using MatchShared.Databases.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
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

	public override Task<bool> Load( CancellationToken token = default )
	{
		if( IsLoaded )
		{
			return Task.FromResult( IsLoadedInternal );
		}

		var connectionString = new ConnectionString
		{
			Connection = ConnectionType.Direct,
			Filename = SharedSettings.GetDatabasePath(),
			ReadOnly = IsReadOnly,
		};

		Database = new LiteDatabase( connectionString, Mapper );
		IsLoadedInternal = true;
		return Task.FromResult( IsLoadedInternal );
	}

	public override Task<T> GetData<T>( string dataId = "", CancellationToken token = default )
	{
		var typeName = typeof( T ).Name;
		dataId = string.IsNullOrEmpty( dataId ) ? typeName : dataId;
		return Task.FromResult( Database.GetCollection<T>( typeName ).FindById( dataId ) );
	}

	public override Task<bool> SaveData<T>( T data, CancellationToken token = default ) => SaveData( EnumerableExtensions.AsSingleton( data ), token );
	public override Task<bool> SaveData<T>( IEnumerable<T> datas, CancellationToken token = default )
	{
		return Task.FromResult( Database.GetCollection<T>( typeof( T ).Name ).Upsert( datas ) > 0 );
	}

	protected override void InternalDispose()
	{
		Database?.Dispose();
		Database = null;
	}

	public override Task<bool> DeleteData<T>( string databaseIndex, CancellationToken token = default )
	{
		return Task.FromResult( Database
				.GetCollection<T>( typeof( T ).Name )
				.Delete( databaseIndex ) );
	}

	public override Task<bool> DeleteData<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default )
	{
		return Task.FromResult( Database
				.GetCollection<T>( typeof( T ).Name )
				.DeleteMany( Query.In( "_id", databaseIndexes.Select( id => new BsonValue( id ) ) ) ) > 0 );
	}
}
