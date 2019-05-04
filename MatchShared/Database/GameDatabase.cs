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
		protected readonly object globalDataLock;
		protected GlobalData globalData;
		protected Dictionary<string , MatchData> matchesData;
		protected Dictionary<string , RoundData> roundsData;
		public SharedSettings SharedSettings { get; set; }

		public bool ReadOnly => false;

		protected event LoadGlobalDataDelegate LoadGlobalDataDelegate;

		protected event LoadMatchDataDelegate LoadMatchDataDelegate;

		protected event LoadRoundDataDelegate LoadRoundDataDelegate;

		protected event SaveGlobalDataDelegate SaveGlobalDataDelegate;

		protected event SaveMatchDataDelegate SaveMatchDataDelegate;

		protected event SaveRoundDataDelegate SaveRoundDataDelegate;

		protected GameDatabase()
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

			List<string> matchesOrRounds = matchOrRound ? globalData.Matches : globalData.Rounds;

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
				foreach( string matchName in globalData.Matches )
				{
					//await GetMatchData( matchName , true );
					loadingTasks.Add( GetMatchData( matchName , true ) );
				}

				foreach( string roundName in globalData.Rounds )
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

		public void SaveData<T>( T data , string dataId = "" )
		{
			//bleh

			if( typeof( T ) == typeof( GlobalData ) )
			{

			}
			else if( typeof( T ) == typeof( MatchData ) )
			{

			}
			else if( typeof( T ) == typeof( RoundData ) )
			{

			}
		}

		public T GetData<T>( string dataId = "" )
		{
			/*
			if( typeof( T ) == typeof( GlobalData ) )
			{
				return (T) await GetGlobalData();
			}
			else if( typeof( T ) == typeof( MatchData ) )
			{
				await SaveMatchData( dataId , data as MatchData );
			}
			else if( typeof( T ) == typeof( RoundData ) )
			{
				await SaveRoundData( dataId , data as RoundData );
			}
			*/

			return default;
		}
	}
}