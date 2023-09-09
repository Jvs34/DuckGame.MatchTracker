using System;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{

	public interface IGameDatabase : IDisposable
	{
		SharedSettings SharedSettings { get; set; }

		bool ReadOnly { get; }

		Task Load( CancellationToken token = default );

		Task SaveData<T>( T data , CancellationToken token = default ) where T : IDatabaseEntry;

		Task<T> GetData<T>( string dataId = "" , CancellationToken token = default ) where T : IDatabaseEntry;
	}
}