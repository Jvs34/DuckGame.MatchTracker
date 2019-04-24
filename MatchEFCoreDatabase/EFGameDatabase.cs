using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class EFGameDatabase : IGameDatabase
	{
		private readonly DbContextOptions<GameDatabaseContext> databaseContextOptions;
		private GameDatabaseContext databaseContext;
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
			databaseContext = new GameDatabaseContext( databaseContextOptions );

			RoundData roundData = new RoundData()
			{
				RecordingType = RecordingType.ReplayAndVoiceChat ,
				Players = new List<PlayerData>()
				{
					new PlayerData()
					{
						UserId = "PLAYER1",
						Name = "Player1",
						Team = new TeamData()
						{
							hatName = "Player 1",
						}
					},
					new PlayerData()
					{
						UserId = "PLAYER2",
						Name = "Player2",
						Team = new TeamData()
						{
							hatName = "Player 2",
						}
					},
				} ,
				LevelName = "96af28ba-c1ba-4527-90ae-d393931ca0c3" ,
				TimeStarted = DateTime.Parse( "2018-08-02T15:22:38.6115392+02:00" ) ,
				TimeEnded = DateTime.Parse( "2018-08-02T15:23:11.7635398+02:00" ) ,
				Winner = new TeamData()
				{
					hatName = "Player 2" ,
					score = 1 ,
				} ,
				Name = "2018-08-02 15-22-38" ,
				IsCustomLevel = false ,
			};

			databaseContext.Add( roundData );
			databaseContext.SaveChanges();
		}

		public async Task<GlobalData> GetGlobalData( bool forceRefresh = false )
		{
			await Task.CompletedTask;
			GlobalData globalData = null;
			/*
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
			*/
			return globalData;
		}

		public async Task<MatchData> GetMatchData( string matchName , bool forceRefresh = false )
		{
			await Task.CompletedTask;
			throw new NotImplementedException();
		}

		public async Task<RoundData> GetRoundData( string roundName , bool forceRefresh = false )
		{
			RoundData roundData = null;

			if( !forceRefresh )
			{
				//TODO: find a better way to include everything automatically
				roundData = await databaseContext.RoundDataSet.FirstOrDefaultAsync( round => round.Name.Equals( roundName ) );
			}

			if( roundData == null )
			{
				forceRefresh = true;
			}

			if( forceRefresh && LoadRoundDataDelegate != null )
			{
				roundData = await LoadRoundDataDelegate( this , SharedSettings , roundName );

				if( roundData != null )
				{
					//gotta track this one now
					await databaseContext.AddAsync( roundData );
					await databaseContext.SaveChangesAsync();
				}
			}

			return roundData;
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			await Task.CompletedTask;
			throw new NotImplementedException();
		}

		public async Task Load()
		{
			List<Task> loadingTasks = new List<Task>();

			//can't add this to the tasks as we have to wait for this one before we can actually know to fetch the rest
			GlobalData globalData = await GetGlobalData( true );

			if( globalData != null )
			{
				foreach( String matchName in globalData.Matches )
				{
					loadingTasks.Add( GetMatchData( matchName , true ) );
				}

				foreach( String roundName in globalData.Rounds )
				{
					loadingTasks.Add( GetRoundData( roundName , true ) );
				}
			}

			await Task.WhenAll( loadingTasks );
		}

		public async Task SaveGlobalData( GlobalData globalData )
		{
			await Task.CompletedTask;
			throw new NotImplementedException();
		}

		public async Task SaveMatchData( string matchName , MatchData matchData )
		{
			await Task.CompletedTask;
			throw new NotImplementedException();
		}

		public async Task SaveRoundData( string roundName , RoundData roundData )
		{
			await Task.CompletedTask;
			throw new NotImplementedException();
		}
	}
}