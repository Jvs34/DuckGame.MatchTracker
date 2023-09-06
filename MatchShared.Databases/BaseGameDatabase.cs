using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MatchTracker
{
	public abstract class BaseGameDatabase : IGameDatabase
	{
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();
		public virtual bool ReadOnly => false;
		public virtual bool InitialLoad { get; set; }

		public virtual async Task Load()
		{
			if( InitialLoad )
			{
				await LoadEverything();
			}
		}

		/// <summary>
		/// Loads everything from the EntryData tree
		/// </summary>
		/// <returns></returns>
		protected virtual Task LoadEverything()
		{
			return Task.CompletedTask;
		}

		#region INTERFACE
		public abstract Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry;
		public abstract Task SaveData<T>( T data ) where T : IDatabaseEntry;
		public abstract void Dispose();
		#endregion
	}
}
