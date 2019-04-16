using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class GameDatabase : IGameDatabase
	{
		private readonly object globalDataLock;
		private GlobalData globalData;
		private Dictionary<string , MatchData> matchesData;
		private Dictionary<string , RoundData> roundsData;
		public SharedSettings SharedSettings { get; set; }

		public event LoadGlobalDataDelegate LoadGlobalDataDelegate;

		public event LoadMatchDataDelegate LoadMatchDataDelegate;

		public event LoadRoundDataDelegate LoadRoundDataDelegate;

		public event SaveGlobalDataDelegate SaveGlobalDataDelegate;

		public event SaveMatchDataDelegate SaveMatchDataDelegate;

		public event SaveRoundDataDelegate SaveRoundDataDelegate;

		public GameDatabase()
		{
			SharedSettings = new SharedSettings();
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
					GlobalData globalDataResult = await LoadGlobalDataDelegate( this , SharedSettings );
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

		public async Task<MatchData> GetMatchData( string matchName , bool forceRefresh = false )
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
					matchData = await LoadMatchDataDelegate( this , SharedSettings , matchName );
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

		public async Task<RoundData> GetRoundData( string roundName , bool forceRefresh = false )
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
					roundData = await LoadRoundDataDelegate( this , SharedSettings , roundName );
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

			List<string> matchesOrRounds = matchOrRound ? globalData.matches : globalData.rounds;

			List<Task> callbackTasks = new List<Task>();
			foreach( string matchOrRoundName in matchesOrRounds )
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
				foreach( string matchName in globalData.matches )
				{
					//await GetMatchData( matchName , true );
					loadingTasks.Add( GetMatchData( matchName , true ) );
				}

				foreach( string roundName in globalData.rounds )
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
				await SaveGlobalDataDelegate( this , SharedSettings , globalData );
			}
		}

		public async Task SaveMatchData( string matchName , MatchData matchData )
		{
			lock( matchesData )
			{
				matchesData [matchName] = matchData;
			}
			if( SaveMatchDataDelegate != null )
			{
				await SaveMatchDataDelegate( this , SharedSettings , matchName , matchData );
			}
		}

		public async Task SaveRoundData( string roundName , RoundData roundData )
		{
			lock( roundsData )
			{
				roundsData [roundName] = roundData;
			}
			if( SaveRoundDataDelegate != null )
			{
				await SaveRoundDataDelegate( this , SharedSettings , roundName , roundData );
			}
		}
	}
}