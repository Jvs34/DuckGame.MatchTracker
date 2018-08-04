using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class EFGameDatabase : IGameDatabase
	{
		private readonly DbContextOptions<GameDatabaseContext> databaseContextOptions;
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();

		public event LoadGlobalDataDelegate LoadGlobalDataDelegate;

		public event LoadMatchDataDelegate LoadMatchDataDelegate;

		public event LoadRoundDataDelegate LoadRoundDataDelegate;

		public event SaveGlobalDataDelegate SaveGlobalDataDelegate;

		public event SaveMatchDataDelegate SaveMatchDataDelegate;

		public event SaveRoundDataDelegate SaveRoundDataDelegate;

		public EFGameDatabase()
		{
			databaseContextOptions = new DbContextOptionsBuilder<GameDatabaseContext>().Options;
		}

		public async Task<GlobalData> GetGlobalData( bool forceRefresh = false )
		{
			GlobalData globalData = null;
			using( var databaseContext = new GameDatabaseContext( databaseContextOptions ) )
			{
				//try to get the first globaldata
				globalData = await databaseContext.GlobalDataSet.FirstOrDefaultAsync();
				if( globalData == null )
				{
					forceRefresh = true;
				}

				if( forceRefresh && LoadGlobalDataDelegate != null )
				{
					GlobalData globalDataResult = await LoadGlobalDataDelegate( this , SharedSettings );
					if( globalDataResult != null )
					{
						globalData = globalDataResult;
					}
				}

				if( forceRefresh && globalData != null )
				{
					await databaseContext.SaveChangesAsync();
				}
			}
			return globalData;
		}

		public async Task<MatchData> GetMatchData( string matchName , bool forceRefresh = false )
		{
			throw new NotImplementedException();
		}

		public async Task<RoundData> GetRoundData( string roundName , bool forceRefresh = false )
		{
			throw new NotImplementedException();
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			throw new NotImplementedException();
		}

		public async Task Load()
		{
			List<Task> loadingTasks = new List<Task>();

			//can't add this to the tasks as we have to wait for this one before we can actually know to fetch the rest
			GlobalData globalData = await GetGlobalData( true );

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

		public async Task SaveGlobalData( GlobalData globalData )
		{
			throw new NotImplementedException();
		}

		public async Task SaveMatchData( string matchName , MatchData matchData )
		{
			throw new NotImplementedException();
		}

		public async Task SaveRoundData( string roundName , RoundData roundData )
		{
			throw new NotImplementedException();
		}
	}
}