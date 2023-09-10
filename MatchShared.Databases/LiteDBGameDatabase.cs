using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class LiteDBGameDatabase : BaseGameDatabase, IDisposable
	{
		private bool disposedValue;
		public override bool ReadOnly => false;
		public LiteDatabase Database { get; protected set; }
		protected BsonMapper Mapper { get; } = new BsonMapper();

		public LiteDBGameDatabase()
		{

		}

		protected override void DefineMapping<T>() => Mapper.Entity<T>().Id( x => x.DatabaseIndex );

		public override Task Load( CancellationToken token = default )
		{
			Database = new LiteDatabase( SharedSettings.GetDatabasePath() , Mapper );
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

		public override void Dispose()
		{
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}
