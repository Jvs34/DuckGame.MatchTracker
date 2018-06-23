using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class GameDatabase
	{
		public SharedSettings sharedSettings;
		public GlobalData globalData;
		public Dictionary<string , MatchData> matchesData;
		public Dictionary<string , RoundData> roundsData;
		public event Func<SharedSettings , Task<GlobalData>> LoadGlobalData;
		public event Func<SharedSettings , String , Task<MatchData>> LoadMatchData;
		public event Func<SharedSettings , String , Task<RoundData>> LoadRoundData;

		public GameDatabase()
		{
			sharedSettings = new SharedSettings();
			globalData = null;
			matchesData = new Dictionary<string , MatchData>();
			roundsData = new Dictionary<string , RoundData>();
		}

		public async Task Load()
		{
			List<Task> loadingTasks = new List<Task>();

			//can't add this to the tasks as we have to wait for this one before we can actually know to fetch the rest
			await GetGlobalData( true );

			if( globalData != null )
			{
				foreach( String matchName in globalData.matches )
				{
					loadingTasks.Add( GetMatchData( matchName , true ) );
				}

				foreach( String roundName in globalData.rounds )
				{
					loadingTasks.Add( GetRoundData( roundName , true ) );
				}
			}

			await Task.WhenAll( loadingTasks );
		}

		public async Task<GlobalData> GetGlobalData( bool forceRefresh = false )
		{
			if( globalData == null )
				forceRefresh = true;

			if( forceRefresh && LoadGlobalData != null )
			{
				globalData = await LoadGlobalData( sharedSettings );
			}

			return globalData;
		}

		public async Task<MatchData> GetMatchData( String matchName , bool forceRefresh = false )
		{
			MatchData matchData = null;

			if( !forceRefresh && matchesData.TryGetValue( matchName , out matchData ) )
			{
				return matchData;
			}
			else
			{
				forceRefresh = true;
			}

			if( forceRefresh && LoadMatchData != null )
			{
				matchData = await LoadMatchData( sharedSettings , matchName );
				matchesData [matchName] = matchData;
			}

			return matchData;
		}

		public async Task<RoundData> GetRoundData( String roundName , bool forceRefresh = false )
		{
			RoundData roundData = null;

			if( !forceRefresh && roundsData.TryGetValue( roundName , out roundData ) )
			{
				return roundData;
			}
			else
			{
				forceRefresh = true;
			}

			if( forceRefresh && LoadRoundData != null )
			{
				roundData = await LoadRoundData( sharedSettings , roundName );
				roundsData [roundName] = roundData;
			}

			return roundData;
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			if( callback == null )
				return;

			GlobalData globalData = await GetGlobalData();

			List<String> matchesOrRounds = matchOrRound ? globalData.matches : globalData.rounds;

			List<Task> callbackTasks = new List<Task>();
			foreach( String matchOrRoundName in matchesOrRounds )
			{
				IWinner iterateItem = matchOrRound ?
					await GetMatchData( matchOrRoundName ) as IWinner :
					await GetRoundData( matchOrRoundName ) as IWinner;

				callbackTasks.Add( callback( iterateItem ) );
			}


			await Task.WhenAll( callbackTasks );
		}
	}
}