using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class GameDatabase
	{
		public SharedSettings sharedSettings;
		private readonly Object globalDataLock;
		private GlobalData globalData;
		private Dictionary<string , MatchData> matchesData;
		private Dictionary<string , RoundData> roundsData;

		public event Func<GameDatabase , SharedSettings , Task<GlobalData>> LoadGlobalDataDelegate;

		public event Func<GameDatabase , SharedSettings , String , Task<MatchData>> LoadMatchDataDelegate;

		public event Func<GameDatabase , SharedSettings , String , Task<RoundData>> LoadRoundDataDelegate;

		public event Func<GameDatabase , SharedSettings , GlobalData , Task> SaveGlobalDataDelegate;

		public event Func<GameDatabase , SharedSettings , String , MatchData , Task> SaveMatchDataDelegate;

		public event Func<GameDatabase , SharedSettings , String , RoundData , Task> SaveRoundDataDelegate;

		public GameDatabase()
		{
			sharedSettings = new SharedSettings();
			globalData = null;
			globalDataLock = new object();
			matchesData = new Dictionary<string , MatchData>();
			roundsData = new Dictionary<string , RoundData>();
		}

		public async Task<GlobalData> GetGlobalData( bool forceRefresh = false )
		{
			if( globalData == null )
				forceRefresh = true;

			if( forceRefresh && LoadGlobalDataDelegate != null )
			{
				try
				{
					GlobalData globalDataResult = await LoadGlobalDataDelegate( this , sharedSettings );
					if( globalDataResult != null )
					{
						lock( globalDataLock )
						{
							globalData = globalDataResult;
						}
					}
				}
				catch( Exception e )
				{
					Console.WriteLine( e );
				}
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

			if( forceRefresh && LoadMatchDataDelegate != null )
			{
				try
				{
					matchData = await LoadMatchDataDelegate( this , sharedSettings , matchName );
					if( matchData != null )
					{
						lock( matchesData )
						{
							matchesData [matchName] = matchData;
						}
					}
				}
				catch( Exception e )
				{
					Console.WriteLine( e );
				}
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

			if( forceRefresh && LoadRoundDataDelegate != null )
			{
				try
				{
					roundData = await LoadRoundDataDelegate( this , sharedSettings , roundName );
					if( roundData != null )
					{
						lock( roundsData )
						{
							roundsData [roundName] = roundData;
						}
					}
				}
				catch( Exception e )
				{
					Console.WriteLine( e );
				}
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

		public async Task Load()
		{
			List<Task> loadingTasks = new List<Task>();

			//can't add this to the tasks as we have to wait for this one before we can actually know to fetch the rest
			await GetGlobalData( true );

			if( globalData != null )
			{
				foreach( String matchName in globalData.matches )
				{
					//await GetMatchData( matchName , true );
					loadingTasks.Add( GetMatchData( matchName , true ) );
				}

				foreach( String roundName in globalData.rounds )
				{
					//await GetRoundData( roundName , true );
					loadingTasks.Add( GetRoundData( roundName , true ) );
				}
			}

			await Task.WhenAll( loadingTasks );
		}

		public async Task SaveGlobalData( GlobalData globalData )
		{
			await Task.CompletedTask;

			lock( globalDataLock )
			{
				this.globalData = globalData;
			}
			if( SaveGlobalDataDelegate != null )
			{
				await SaveGlobalDataDelegate( this , sharedSettings , globalData );
			}
		}

		public async Task SaveMatchData( String matchName , MatchData matchData )
		{
			lock( matchesData )
			{
				matchesData [matchName] = matchData;
			}
			if( SaveMatchDataDelegate != null )
			{
				await SaveMatchDataDelegate( this , sharedSettings , matchName , matchData );
			}
		}

		public async Task SaveRoundData( String roundName , RoundData roundData )
		{
			lock( roundsData )
			{
				roundsData [roundName] = roundData;
			}
			if( SaveRoundDataDelegate != null )
			{
				await SaveRoundDataDelegate( this , sharedSettings , roundName , roundData );
			}
		}
	}
}