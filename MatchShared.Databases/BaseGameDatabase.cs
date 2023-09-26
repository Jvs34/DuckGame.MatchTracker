using System;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public abstract class BaseGameDatabase : IGameDatabase, IDisposable
	{
		protected bool IsDisposed { get; private set; }
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();
		public virtual bool ReadOnly => false;

		protected BaseGameDatabase()
		{
			DefineMapping<IDatabaseEntry>();
			DefineMapping<EntryListData>();
			DefineMapping<RoundData>();
			DefineMapping<MatchData>();
			DefineMapping<TournamentData>();
			DefineMapping<LevelData>();
			DefineMapping<TagData>();
			DefineMapping<PlayerData>();
			DefineMapping<ObjectData>();
			DefineMapping<DestroyTypeData>();
		}

		public abstract Task Load( CancellationToken token = default );
		protected abstract void DefineMapping<T>() where T : IDatabaseEntry;
		public abstract Task<T> GetData<T>( string dataId = "" , CancellationToken token = default ) where T : IDatabaseEntry;
		public abstract Task SaveData<T>( T data , CancellationToken token = default ) where T : IDatabaseEntry;
		protected abstract void InternalDispose();

		protected virtual void Dispose( bool disposing )
		{
			if( !IsDisposed )
			{
				if( disposing )
				{
					InternalDispose();
				}

				IsDisposed = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}
