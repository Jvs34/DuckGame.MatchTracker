using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public abstract class BaseGameDatabase : IGameDatabase
	{
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();
		public virtual bool ReadOnly => false;

		protected BaseGameDatabase()
		{
			DefineMapping<IDatabaseEntry>();
			DefineMapping<EntryListData>();
			DefineMapping<RoundData>();
			DefineMapping<MatchData>();
			DefineMapping<LevelData>();
			DefineMapping<TagData>();
			DefineMapping<PlayerData>();
		}

		public abstract Task Load( CancellationToken token = default );

		#region INTERFACE
		protected abstract void DefineMapping<T>() where T : IDatabaseEntry;
		public abstract Task<T> GetData<T>( string dataId = "" , CancellationToken token = default ) where T : IDatabaseEntry;
		public abstract Task SaveData<T>( T data , CancellationToken token = default ) where T : IDatabaseEntry;
		public abstract void Dispose();
		#endregion
	}
}
