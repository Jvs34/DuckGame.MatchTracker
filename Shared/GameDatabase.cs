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
			await GetGlobalData( true );

			if( globalData != null )
			{
				foreach( String matchName in globalData.matches )
				{
					await GetMatchData( matchName , true );
				}
			}

			if( globalData != null )
			{
				foreach( String roundName in globalData.rounds )
				{
					await GetRoundData( roundName , true );
				}
			}
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
	}
}