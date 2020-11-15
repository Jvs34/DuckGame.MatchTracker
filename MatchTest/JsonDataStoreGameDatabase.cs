using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MatchTest
{
	public class JsonDataStoreGameDatabase : IGameDatabase, IDisposable
	{
		public SharedSettings SharedSettings { get; set; }

		public bool ReadOnly => false;



		public JsonDataStoreGameDatabase()
		{

		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			return default;
		}

		public async Task Load()
		{

		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{

		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects).

				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~JsonDataStoreGameDatabase()
		// {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
