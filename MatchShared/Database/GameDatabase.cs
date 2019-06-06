using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchTracker
{
	public delegate Task<GlobalData> LoadGlobalDataDelegate( IGameDatabase database , SharedSettings settings );
	public delegate Task<MatchData> LoadMatchDataDelegate( IGameDatabase database , SharedSettings settings , string matchName );
	public delegate Task<RoundData> LoadRoundDataDelegate( IGameDatabase database , SharedSettings settings , string roundName );
	public delegate Task SaveGlobalDataDelegate( IGameDatabase database , SharedSettings settings , GlobalData globalData );
	public delegate Task SaveMatchDataDelegate( IGameDatabase database , SharedSettings settings , string matchName , MatchData matchData );
	public delegate Task SaveRoundDataDelegate( IGameDatabase database , SharedSettings settings , string roundName , RoundData roundData );

	public class GameDatabase : IGameDatabase
	{
		public SharedSettings SharedSettings { get; set; }

		public virtual bool ReadOnly => false;

		protected event LoadGlobalDataDelegate LoadGlobalDataDelegate;

		protected event LoadMatchDataDelegate LoadMatchDataDelegate;

		protected event LoadRoundDataDelegate LoadRoundDataDelegate;

		protected event SaveGlobalDataDelegate SaveGlobalDataDelegate;

		protected event SaveMatchDataDelegate SaveMatchDataDelegate;

		protected event SaveRoundDataDelegate SaveRoundDataDelegate;

		protected GameDatabase()
		{
			SharedSettings = new SharedSettings();
		}

		public async Task<GlobalData> GetGlobalData()
		{
			return await LoadGlobalDataDelegate( this , SharedSettings );
		}

		public async Task<MatchData> GetMatchData( string matchName )
		{
			return await LoadMatchDataDelegate( this , SharedSettings , matchName );
		}

		public async Task<RoundData> GetRoundData( string roundName )
		{
			return await LoadRoundDataDelegate( this , SharedSettings , roundName );
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task<bool>> callback )
		{
			if( callback == null )
				return;

			GlobalData globalData = await GetData<GlobalData>();

			foreach( string matchOrRoundName in matchOrRound ? globalData.Matches : globalData.Rounds )
			{
				IWinner iterateItem = matchOrRound ?
					await GetData<MatchData>( matchOrRoundName ) as IWinner :
					await GetData<RoundData>( matchOrRoundName ) as IWinner;

				bool shouldContinue = await callback( iterateItem );

				if( !shouldContinue )
				{
					break;
				}
			}
		}

		public async Task Load()
		{
		}

		public async Task SaveGlobalData( GlobalData globalData )
		{
			await SaveGlobalDataDelegate( this , SharedSettings , globalData );
		}

		public async Task SaveMatchData( string matchName , MatchData matchData )
		{
			await SaveMatchDataDelegate( this , SharedSettings , matchName , matchData );
		}

		public async Task SaveRoundData( string roundName , RoundData roundData )
		{
			await SaveRoundDataDelegate( this , SharedSettings , roundName , roundData );
		}


		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			if( typeof( T ) == typeof( GlobalData ) )
			{
				await SaveGlobalData( data as GlobalData );
			}
			else if( typeof( T ) == typeof( MatchData ) )
			{
				await SaveMatchData( data.DatabaseIndex , data as MatchData );
			}
			else if( typeof( T ) == typeof( RoundData ) )
			{
				await SaveRoundData( data.DatabaseIndex , data as RoundData );
			}
			else
			{
				throw new NotImplementedException( $"Missing check clause for GameDatabase SaveData{typeof( T )}!" );
			}
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			if( typeof( T ) == typeof( GlobalData ) )
			{
				return (T) (object) await GetGlobalData();
			}
			else if( typeof( T ) == typeof( MatchData ) )
			{
				return (T) (object) await GetMatchData( dataId );
			}
			else if( typeof( T ) == typeof( RoundData ) )
			{
				return (T) (object) await GetRoundData( dataId );
			}
			else
			{
				throw new NotImplementedException( $"Missing check clause for GameDatabase GetData{typeof( T )}!" );
			}
		}

	}
}