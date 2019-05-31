using System;
using System.Threading.Tasks;

namespace MatchTracker
{

	public interface IGameDatabase
	{
		SharedSettings SharedSettings { get; set; }

		bool ReadOnly { get; }

		Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback );

		Task Load();

		Task SaveData<T>( T data ) where T : IDatabaseEntry;

		Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry;
	}
}