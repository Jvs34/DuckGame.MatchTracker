using System;
using System.Threading.Tasks;

namespace MatchTracker
{
	public delegate Task<GlobalData> LoadGlobalDataDelegate( IGameDatabase database , SharedSettings settings );
	public delegate Task<MatchData> LoadMatchDataDelegate( IGameDatabase database , SharedSettings settings , string matchName );
	public delegate Task<RoundData> LoadRoundDataDelegate( IGameDatabase database , SharedSettings settings , string roundName );
	public delegate Task SaveGlobalDataDelegate( IGameDatabase database , SharedSettings settings , GlobalData globalData );
	public delegate Task SaveMatchDataDelegate( IGameDatabase database , SharedSettings settings , string matchName , MatchData matchData );
	public delegate Task SaveRoundDataDelegate( IGameDatabase database , SharedSettings settings , string roundName , RoundData roundData );

	public interface IGameDatabase
	{
		SharedSettings SharedSettings { get; set; }

		event LoadGlobalDataDelegate LoadGlobalDataDelegate;

		event LoadMatchDataDelegate LoadMatchDataDelegate;

		event LoadRoundDataDelegate LoadRoundDataDelegate;

		event SaveGlobalDataDelegate SaveGlobalDataDelegate;

		event SaveMatchDataDelegate SaveMatchDataDelegate;

		event SaveRoundDataDelegate SaveRoundDataDelegate;

		Task<GlobalData> GetGlobalData( bool forceRefresh = false );

		Task<MatchData> GetMatchData( string matchName , bool forceRefresh = false );

		Task<RoundData> GetRoundData( string roundName , bool forceRefresh = false );

		Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback );

		Task Load();

		Task SaveGlobalData( GlobalData globalData );

		Task SaveMatchData( string matchName , MatchData matchData );

		Task SaveRoundData( string roundName , RoundData roundData );
	}
}