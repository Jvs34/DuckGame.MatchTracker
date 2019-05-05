using System;
using System.Threading.Tasks;

namespace MatchTracker
{

	public interface IGameDatabase
	{
		SharedSettings SharedSettings { get; set; }

		bool ReadOnly { get; }

		Task<GlobalData> GetGlobalData( bool forceRefresh = false );

		Task<MatchData> GetMatchData( string matchName , bool forceRefresh = false );

		Task<RoundData> GetRoundData( string roundName , bool forceRefresh = false );

		Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback );

		Task Load();

		Task SaveGlobalData( GlobalData globalData );

		Task SaveMatchData( string matchName , MatchData matchData );

		Task SaveRoundData( string roundName , RoundData roundData );

		void SaveData<T>( T data , string dataId = "" );

		T GetData<T>( string dataId = "" );
	}
}