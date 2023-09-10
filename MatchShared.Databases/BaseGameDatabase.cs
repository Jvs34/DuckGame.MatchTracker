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
		public virtual bool InitialLoad { get; set; }

		protected BaseGameDatabase( SharedSettings sharedSettings )
		{
			SharedSettings = sharedSettings;
		}

		public virtual async Task Load( CancellationToken token = default )
		{
			if( InitialLoad )
			{
				await LoadEverything( token );
			}
		}

		/// <summary>
		/// Loads everything from the EntryData tree
		/// </summary>
		/// <returns></returns>
		protected virtual Task LoadEverything( CancellationToken token = default )
		{
			return Task.CompletedTask;
		}

		#region INTERFACE
		public abstract Task<T> GetData<T>( string dataId = "" , CancellationToken token = default ) where T : IDatabaseEntry;
		public abstract Task SaveData<T>( T data , CancellationToken token = default ) where T : IDatabaseEntry;
		public abstract void Dispose();
		#endregion
	}
}
