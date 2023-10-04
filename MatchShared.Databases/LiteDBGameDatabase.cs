using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class LiteDBGameDatabase : BaseGameDatabase, IDisposable
	{
		public override bool ReadOnly { get; }
		public LiteDatabase Database { get; protected set; }
		protected BsonMapper Mapper { get; } = new BsonMapper();

		public LiteDBGameDatabase() : this( false ) { }
		public LiteDBGameDatabase( bool readOnly ) => ReadOnly = readOnly;

		protected override void DefineMapping<T>() => Mapper.Entity<T>().Id( x => x.DatabaseIndex );

		public override Task Load( CancellationToken token = default )
		{
			var connectionString = new ConnectionString
			{
				Connection = ConnectionType.Direct ,
				Filename = SharedSettings.GetDatabasePath() ,
				ReadOnly = ReadOnly ,
			};

			Database = new LiteDatabase( connectionString , Mapper );
			return Task.CompletedTask;
		}

		public override Task<T> GetData<T>( string dataId = "" , CancellationToken token = default )
		{
			if( string.IsNullOrEmpty( dataId ) )
			{
				dataId = typeof( T ).Name;
			}

			var collection = Database.GetCollection<T>( typeof( T ).Name );

			T data = collection.FindById( dataId );
			return Task.FromResult( data );
		}

		public override Task SaveData<T>( T data , CancellationToken token = default )
		{
			var collection = Database.GetCollection<T>( data.GetType().Name );
			collection.Upsert( data );
			return Task.CompletedTask;
		}

		protected override void InternalDispose()
		{
			Database?.Dispose();
			Database = null;
		}
	}
}
